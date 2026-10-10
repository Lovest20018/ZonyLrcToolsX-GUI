using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Lyrics;
using ZonyLrcTools.Desktop.Infrastructure.Localization;
using ZonyLrcTools.Desktop.Services;

namespace ZonyLrcTools.Desktop.ViewModels;

public partial class ManualDownloadViewModel : ViewModelBase
{
    private readonly ILyricsDownloader _downloader;
    private readonly IDialogService _dialogs;
    private readonly AvaloniaWarpLogger _logger;
    private readonly GlobalOptions _options;
    private CancellationTokenSource? _cts;
    private LyricsItemCollection? _lyrics;
    private long _inputVersion;
    private string _statusKey = "Work_ManualReady";
    private object[] _statusArgs = [];
    private bool _isSaving;
    [ObservableProperty] private string _songName = "";
    [ObservableProperty] private string _artist = "";
    [ObservableProperty] private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    [ObservableProperty] private string _preview = "";
    [ObservableProperty] private string _matchedName = "";
    [ObservableProperty] private string _matchedArtist = "";
    [ObservableProperty] private string _matchedProvider = "";
    [ObservableProperty] private int _lineCount;
    [ObservableProperty] private bool _showRawLrc;
    [ObservableProperty] private bool _isStopping;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasLyrics;
    public ObservableCollection<LyricsItem> PreviewLines { get; } = new();
    public string Status => Text.Format(_statusKey, _statusArgs);
    public string ResultSummary => Text.Format("Work_QuerySummary", MatchedName, MatchedArtist, MatchedProvider, LineCount);
    public string SaveFileName => HasLyrics ? SafeName(MatchedArtist, MatchedName) + ".lrc" : "";
    public string SavePath => HasLyrics ? Path.Combine(OutputFolder, SaveFileName) : OutputFolder;
    public string StopText => Text[IsStopping ? "Work_Stopping" : "Work_StopMatch"];
    public bool HasNoResult => !HasLyrics && !IsBusy;
    [RelayCommand] private void OpenSettings()
    {
        if (App.Services?.GetService<MainWindowViewModel>() is { } main) main.SelectedNavigationIndex = 5;
    }
    public ManualDownloadViewModel(ILyricsDownloader downloader, IDialogService dialogs, AvaloniaWarpLogger logger,
        IOptions<GlobalOptions> options, IUILocalizationService? localization = null)
    {
        _downloader = downloader;
        _dialogs = dialogs;
        _logger = logger;
        _options = options.Value;
        if (localization != null) localization.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ResultSummary));
            OnPropertyChanged(nameof(StopText));
        };
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MatchedName) or nameof(MatchedArtist) or nameof(MatchedProvider) or nameof(LineCount))
                OnPropertyChanged(nameof(ResultSummary));
            if (e.PropertyName is nameof(HasLyrics) or nameof(MatchedName) or nameof(MatchedArtist) or nameof(OutputFolder))
            {
                OnPropertyChanged(nameof(SaveFileName));
                OnPropertyChanged(nameof(SavePath));
            }
            if (e.PropertyName is nameof(HasLyrics) or nameof(IsBusy)) OnPropertyChanged(nameof(HasNoResult));
            if (e.PropertyName == nameof(IsBusy)) { MatchCommand.NotifyCanExecuteChanged(); SaveCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged(); }
        };
    }
    private void SetStatus(string key, params object[] arguments)
    {
        _statusKey = key;
        _statusArgs = arguments;
        OnPropertyChanged(nameof(Status));
    }
    partial void OnSongNameChanged(string value) => InvalidateInput();
    partial void OnArtistChanged(string value) => InvalidateInput();
    partial void OnIsStoppingChanged(bool value)
    {
        OnPropertyChanged(nameof(StopText));
        CancelCommand.NotifyCanExecuteChanged();
    }
    private void InvalidateInput()
    {
        _inputVersion++;
        if (HasLyrics || IsBusy || _isSaving) SetStatus("Work_InputChanged");
        HasLyrics = false;
        _lyrics = null;
        Preview = "";
        PreviewLines.Clear();
        MatchedName = MatchedArtist = MatchedProvider = "";
        LineCount = 0;
    }
    private bool CanMatch => !IsBusy && !_isSaving;
    private bool CanSave => !IsBusy && !_isSaving && HasLyrics;
    [RelayCommand(CanExecute = nameof(CanMatch))]
    private async Task MatchAsync()
    {
        if (string.IsNullOrWhiteSpace(SongName) || string.IsNullOrWhiteSpace(Artist))
        {
            SetStatus("Work_RequiredQuery");
            return;
        }
        var version = _inputVersion;
        var queryName = SongName.Trim();
        var queryArtist = Artist.Trim();
        IsBusy = true;
        IsStopping = false;
        HasLyrics = false;
        Preview = "";
        PreviewLines.Clear();
        _lyrics = null;
        _cts = new();
        try
        {
            var providers = _downloader.AvailableProviders.ToList();
            if (providers.Count == 0) { SetStatus("Work_EnableSource"); return; }
            foreach (var provider in providers)
            {
                _cts.Token.ThrowIfCancellationRequested();
                if (version != _inputVersion) return;
                SetStatus("Work_MatchingSource", provider.DownloaderName);
                try
                {
                    var lyrics = await provider.DownloadAsync(queryName, queryArtist).AsTask().WaitAsync(_cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();
                    if (version != _inputVersion) return;
                    if (lyrics.IsPruneMusic) continue;
                    _lyrics = lyrics;
                    MatchedName = queryName;
                    MatchedArtist = queryArtist;
                    MatchedProvider = provider.DownloaderName;
                    LineCount = lyrics.Count;
                    Preview = lyrics.ToString();
                    foreach (var line in lyrics) PreviewLines.Add(line);
                    HasLyrics = true;
                    SetStatus("Work_Matched");
                    return;
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested) { throw; }
                catch (Exception ex) { await _logger.WarnAsync(Text.Format("Work_SourceError", provider.DownloaderName, ex.Message)); }
            }
            if (version == _inputVersion) SetStatus("Work_NoMatch");
        }
        catch (OperationCanceledException) { SetStatus("Work_MatchStopped"); }
        catch (Exception ex) { SetStatus("Work_MatchError", ex.Message); }
        finally { IsBusy = false; IsStopping = false; _cts.Dispose(); _cts = null; }
    }
    private bool CanCancel => IsBusy && !IsStopping;
    [RelayCommand(CanExecute = nameof(CanCancel))] private void Cancel()
    {
        IsStopping = true;
        SetStatus("Work_Stopping");
        _cts?.Cancel();
    }
    [RelayCommand] private async Task BrowseAsync()
    {
        var folder = await _dialogs.ShowFolderPickerAsync(Text["Work_SaveFolder"]);
        if (folder != null) OutputFolder = folder;
    }
    private static string SafeName(string artist, string name) => string.Concat($"{artist} - {name}"
        .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).TrimEnd('.', ' ');
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_lyrics == null || string.IsNullOrWhiteSpace(OutputFolder)) return;
        var version = _inputVersion;
        var lyrics = _lyrics;
        var path = Path.Combine(OutputFolder, SaveFileName);
        _isSaving = true;
        MatchCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        try
        {
            if (File.Exists(path) && !await _dialogs.ShowConfirmDialogAsync(Text["Work_OverwriteTitle"], Text.Format("Work_OverwritePrompt", path))) return;
            if (version != _inputVersion || !HasLyrics || !ReferenceEquals(_lyrics, lyrics)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var encoding = _options.Provider.Lyric.Config.FileEncoding;
            var selected = encoding == "utf-8-bom" ? new UTF8Encoding(true) : encoding == "utf-8" ? new UTF8Encoding(false) : Encoding.GetEncoding(encoding);
            var bytes = selected.GetPreamble().Concat(selected.GetBytes(lyrics.ToString())).ToArray();
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporary, bytes);
                if (version != _inputVersion || !HasLyrics) return;
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            SetStatus("Work_Saved", path);
        }
        catch (Exception ex) { SetStatus("Work_SaveError", ex.Message); }
        finally
        {
            _isSaving = false;
            MatchCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}
