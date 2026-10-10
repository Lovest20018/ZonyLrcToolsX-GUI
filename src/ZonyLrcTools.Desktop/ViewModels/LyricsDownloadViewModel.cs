using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using ZonyLrcTools.Common;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.IO;
using ZonyLrcTools.Common.Infrastructure.Threading;
using ZonyLrcTools.Common.Lyrics;
using ZonyLrcTools.Common.TagInfo;
using ZonyLrcTools.Desktop.Infrastructure.Localization;
using ZonyLrcTools.Desktop.Services;

namespace ZonyLrcTools.Desktop.ViewModels;

public partial class LyricsDownloadViewModel : ViewModelBase
{
    private readonly ILyricsDownloader _lyricsDownloader;
    private readonly IDialogService _dialogs;
    private readonly IFileScanner _scanner;
    private readonly ITagLoader _tags;
    private readonly GlobalOptions _options;
    private readonly Dictionary<string, MusicFileViewModel> _rows = new();
    private readonly List<MusicInfo> _infos = new();
    private CancellationTokenSource? _downloadCts;
    private long _previewVersion;
    private string _statusKey = "Work_BatchReady";
    private object[] _statusArgs = [];
    private string? _previewMessageKey = "Work_SelectPreview";

    [ObservableProperty] private string? _selectedFolderPath;
    [ObservableProperty] private int _parallelCount = 2;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _completedCount;
    [ObservableProperty] private int _failedCount;
    [ObservableProperty] private int _skippedCount;
    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private MusicFileViewModel? _selectedMusicFile;
    [ObservableProperty] private string _previewText = "";
    [ObservableProperty] private bool _isPreviewOpen;
    [ObservableProperty] private bool _showPathColumn;
    [ObservableProperty] private bool _isStopping;
    [ObservableProperty] private bool _isRetryRun;
    [ObservableProperty] private int _scanProgressCount;
    [ObservableProperty] private int _scanTotalCount;
    [ObservableProperty] private int _unrecognizedCount;
    [ObservableProperty] private bool _hasScanned;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryFailedCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelDownloadCommand))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    [NotifyPropertyChangedFor(nameof(CanConfigure))]
    [NotifyPropertyChangedFor(nameof(IsGridReadOnly))]
    private bool _isDownloading;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryFailedCommand))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    [NotifyPropertyChangedFor(nameof(CanConfigure))]
    [NotifyPropertyChangedFor(nameof(IsGridReadOnly))]
    private bool _isScanning;

    public virtual bool IsLyrics => true;
    public virtual string LyricsTitle => Text[IsLyrics ? "Work_BatchTitle" : "Work_AlbumTitle"];
    public virtual string LyricsDescription => Text[IsLyrics ? "Work_BatchDescription" : "Work_AlbumDescription"];
    public string TaskStatus => Text.Format(_statusKey, _statusArgs);
    public bool CanConfigure => !IsDownloading && !IsScanning;
    public bool IsGridReadOnly => !CanConfigure;
    public bool CanStartDownload => CanConfigure && MusicFiles.Count > 0;
    public bool HasNoFiles => MusicFiles.Count == 0;
    public bool HasFailures => MusicFiles.Any(f => f.IsFailure);
    public int ProcessedCount => CompletedCount + SkippedCount + FailedCount;
    public int UnfinishedCount => Math.Max(0, TotalCount - ProcessedCount);
    public string CountsText => Text.Format(IsRetryRun ? "Work_RetryCounts" : "Work_Counts",
        ProcessedCount, TotalCount, CompletedCount, SkippedCount, FailedCount, UnfinishedCount);
    public string EmptyStateText => Text[IsScanning ? "Work_Scanning" : HasScanned ? "Work_NoRecognized" : "Work_EmptyBatch"];
    public string ScanSummary => Text.Format("Work_ScanSummary", ScanProgressCount, MusicFiles.Count, UnrecognizedCount);
    public string OutputPolicy => Text[!IsLyrics || _options.Provider.Lyric.Config.IsSkipExistLyricFiles ? "Work_OutputSkip" : "Work_OutputReplace"];
    public string StopText => Text[IsStopping ? "Work_Stopping" : "Work_Stop"];
    public ObservableCollection<MusicFileViewModel> MusicFiles { get; } = new();

    public LyricsDownloadViewModel(ILyricsDownloader lyricsDownloader, IDialogService dialogService,
        IFileScanner fileScanner, ITagLoader tagLoader, IOptions<GlobalOptions> options,
        IUILocalizationService? localization = null)
    {
        _lyricsDownloader = lyricsDownloader;
        _dialogs = dialogService;
        _scanner = fileScanner;
        _tags = tagLoader;
        _options = options.Value;
        PreviewText = Text["Work_SelectPreview"];
        if (localization != null) localization.LanguageChanged += (_, _) => RefreshLanguage();
        MusicFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasNoFiles));
            OnPropertyChanged(nameof(CanStartDownload));
            OnPropertyChanged(nameof(ScanSummary));
            StartDownloadCommand.NotifyCanExecuteChanged();
            NotifyFailures();
        };
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(CompletedCount) or nameof(FailedCount) or nameof(SkippedCount) or nameof(TotalCount) or nameof(IsRetryRun))
            {
                OnPropertyChanged(nameof(CountsText));
                OnPropertyChanged(nameof(ProcessedCount));
                OnPropertyChanged(nameof(UnfinishedCount));
            }
        };
    }

    private void RefreshLanguage()
    {
        foreach (var name in new[] { nameof(LyricsTitle), nameof(LyricsDescription), nameof(TaskStatus), nameof(CountsText),
            nameof(EmptyStateText), nameof(ScanSummary), nameof(OutputPolicy), nameof(StopText) }) OnPropertyChanged(name);
        foreach (var row in MusicFiles) row.RefreshStatus(Text);
        if (_previewMessageKey != null) PreviewText = Text[_previewMessageKey];
    }
    private void SetStatus(string key, params object[] arguments)
    {
        _statusKey = key;
        _statusArgs = arguments;
        OnPropertyChanged(nameof(TaskStatus));
    }
    partial void OnIsStoppingChanged(bool value)
    {
        OnPropertyChanged(nameof(StopText));
        CancelDownloadCommand.NotifyCanExecuteChanged();
    }
    partial void OnSelectedMusicFileChanged(MusicFileViewModel? value)
    {
        IsPreviewOpen = value != null && IsLyrics;
        if (IsLyrics) _ = LoadPreviewAsync(value);
    }
    partial void OnParallelCountChanged(int value)
    {
        if (value is < 1 or > 32) ParallelCount = Math.Clamp(value, 1, 32);
    }
    partial void OnHasScannedChanged(bool value) => OnPropertyChanged(nameof(EmptyStateText));
    partial void OnScanProgressCountChanged(int value) => OnPropertyChanged(nameof(ScanSummary));
    partial void OnUnrecognizedCountChanged(int value) => OnPropertyChanged(nameof(ScanSummary));

    [RelayCommand(CanExecute = nameof(CanConfigure))]
    private async Task SelectFolderAsync()
    {
        var folder = await _dialogs.ShowFolderPickerAsync(Text["Work_SelectFolder"]);
        if (folder != null) await ScanFolderAsync(folder);
    }
    public async Task ScanFolderAsync(string folder)
    {
        if (!CanConfigure) return;
        SelectedFolderPath = folder;
        IsScanning = true;
        HasScanned = false;
        MusicFiles.Clear();
        _rows.Clear();
        _infos.Clear();
        SelectedMusicFile = null;
        TotalCount = CompletedCount = FailedCount = SkippedCount = ScanProgressCount = ScanTotalCount = UnrecognizedCount = 0;
        ProgressPercentage = 0;
        IsRetryRun = false;
        SetStatus("Work_Scanning");
        OnPropertyChanged(nameof(EmptyStateText));
        try
        {
            var files = (await Task.Run(() => _scanner.ScanMusicFilesAsync(folder, _options.SupportFileExtensions))).ToList();
            ScanTotalCount = files.Count;
            using var tasks = new WarpTask(ParallelCount);
            await Task.WhenAll(files.Select(file => tasks.RunAsync(async () =>
            {
                MusicInfo? info = null;
                try { info = await Task.Run(async () => await _tags.LoadTagAsync(file)); }
                catch (Exception) { /* An unreadable tag does not abort the scan. */ }
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (info != null && (!string.IsNullOrWhiteSpace(info.Name) || !string.IsNullOrWhiteSpace(info.Artist)))
                    {
                        var row = new MusicFileViewModel { FilePath = info.FilePath, Name = info.Name, Artist = info.Artist };
                        row.RefreshStatus(Text);
                        MusicFiles.Add(row);
                        _rows[info.FilePath] = row;
                        _infos.Add(info);
                    }
                    else UnrecognizedCount++;
                    ScanProgressCount++;
                });
            })));
            TotalCount = MusicFiles.Count;
            HasScanned = true;
            SetStatus(MusicFiles.Count == 0 ? "Work_ScanEmpty" : "Work_ScanReady", ScanProgressCount, TotalCount, UnrecognizedCount);
        }
        catch (Exception ex) { SetStatus("Work_ScanError", ex.Message); }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(nameof(EmptyStateText));
            OnPropertyChanged(nameof(OutputPolicy));
            RetryFailedCommand.NotifyCanExecuteChanged();
        }
    }
    [RelayCommand(CanExecute = nameof(CanStartDownload))]
    private Task StartDownloadAsync() => RunDownloadAsync(_infos.ToList(), false);
    private bool CanRetryFailed => CanStartDownload && HasFailures;
    [RelayCommand(CanExecute = nameof(CanRetryFailed))]
    private Task RetryFailedAsync() => RunDownloadAsync(_infos.Where(i => _rows[i.FilePath].IsFailure).ToList(), true);
    protected virtual Task DownloadCoreAsync(List<MusicInfo> infos, int parallelCount, Func<MusicInfo, Task> callback,
        CancellationToken cancellationToken) => _lyricsDownloader.DownloadAsync(infos, parallelCount, callback, cancellationToken);
    private async Task RunDownloadAsync(List<MusicInfo> infos, bool retry)
    {
        if (infos.Count == 0 || !CanConfigure) return;
        if (IsLyrics && !_options.Provider.Lyric.Plugin.Any(p => p.Priority >= 0))
        {
            SetStatus("Work_EnableSource");
            return;
        }
        IsDownloading = true;
        IsStopping = false;
        IsRetryRun = retry;
        CompletedCount = FailedCount = SkippedCount = 0;
        ProgressPercentage = 0;
        TotalCount = infos.Count;
        SetStatus(IsLyrics ? "Work_DownloadingLyrics" : "Work_DownloadingCovers");
        foreach (var info in infos)
        {
            var row = _rows[info.FilePath];
            info.Name = row.Name.Trim();
            info.Artist = row.Artist.Trim();
            row.IsProcessed = row.IsSuccessful = row.IsSkipped = false;
            row.RefreshStatus(Text);
        }
        NotifyFailures();
        _downloadCts = new();
        try
        {
            await DownloadCoreAsync(infos, ParallelCount, async info =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_rows.TryGetValue(info.FilePath, out var row))
                    {
                        row.IsProcessed = true;
                        row.IsSuccessful = info.IsSuccessful;
                        row.IsSkipped = info.IsPruneMusic || info.IsSkipped;
                        row.RefreshStatus(Text);
                        if (ReferenceEquals(row, SelectedMusicFile) && IsLyrics) _ = LoadPreviewAsync(row);
                    }
                    if (info.IsPruneMusic || info.IsSkipped) SkippedCount++;
                    else if (info.IsSuccessful) CompletedCount++;
                    else FailedCount++;
                    ProgressPercentage = ProcessedCount * 100.0 / TotalCount;
                    NotifyFailures();
                });
            }, _downloadCts.Token);
            SetStatus("Work_Completed");
        }
        catch (OperationCanceledException) { SetStatus("Work_Stopped"); }
        catch (Exception ex) { SetStatus("Work_DownloadError", ex.Message); }
        finally
        {
            IsDownloading = false;
            IsStopping = false;
            _downloadCts.Dispose();
            _downloadCts = null;
            NotifyFailures();
        }
    }
    private void NotifyFailures()
    {
        OnPropertyChanged(nameof(HasFailures));
        RetryFailedCommand.NotifyCanExecuteChanged();
    }
    private async Task LoadPreviewAsync(MusicFileViewModel? selected)
    {
        var version = ++_previewVersion;
        var message = "Work_NoLocalLyrics";
        string? result = null;
        if (selected == null) message = "Work_SelectPreview";
        else
        {
            try
            {
                var path = Path.ChangeExtension(selected.FilePath, ".lrc");
                if (File.Exists(path))
                {
                    _previewMessageKey = "Work_LoadingPreview";
                    PreviewText = Text[_previewMessageKey];
                    var encoding = _options.Provider.Lyric.Config.FileEncoding;
                    using var reader = new StreamReader(path, Encoding.GetEncoding(encoding == "utf-8-bom" ? "utf-8" : encoding), true);
                    result = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex) { result = Text.Format("Work_PreviewError", ex.Message); }
        }
        if (version != _previewVersion || !ReferenceEquals(selected, SelectedMusicFile)) return;
        _previewMessageKey = result == null ? message : null;
        PreviewText = result ?? Text[message];
    }
    [RelayCommand] private void ClosePreview() => IsPreviewOpen = false;
    [RelayCommand] private void ShowPreview() { if (IsLyrics) IsPreviewOpen = true; }
    [RelayCommand] private void OpenFolder()
    {
        if (Directory.Exists(SelectedFolderPath))
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, ArgumentList = { SelectedFolderPath! } });
    }
    [RelayCommand] private async Task CopyPathAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime
            && lifetime.MainWindow?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(SelectedFolderPath ?? "");
    }
    [RelayCommand] private async Task ExportFailedAsync()
    {
        var failed = MusicFiles.Where(f => f.IsFailure).ToList();
        if (failed.Count == 0) return;
        var folder = await _dialogs.ShowFolderPickerAsync(Text["Work_ExportFolder"]);
        if (folder == null) return;
        static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
        try
        {
            var path = Path.Combine(folder, $"{(IsLyrics ? "lyrics" : "covers")}_failed_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            await File.WriteAllLinesAsync(path, new[] { "Song,Artist,FilePath" }.Concat(failed.Select(f =>
                $"{Csv(f.Name)},{Csv(f.Artist)},{Csv(f.FilePath)}")), new UTF8Encoding(true));
            SetStatus("Work_Exported", path);
        }
        catch (Exception ex) { SetStatus("Work_ExportError", ex.Message); }
    }
    private bool CanCancelDownload => IsDownloading && !IsStopping;
    [RelayCommand(CanExecute = nameof(CanCancelDownload))] private void CancelDownload()
    {
        if (_downloadCts == null) return;
        IsStopping = true;
        SetStatus("Work_Stopping");
        _downloadCts.Cancel();
    }
}

public partial class MusicFileViewModel : ObservableObject
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _artist = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFailure))] private bool _isProcessed;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFailure))] private bool _isSuccessful;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFailure))] private bool _isSkipped;
    [ObservableProperty] private string? _statusMessage;
    public bool IsFailure => IsProcessed && !IsSuccessful && !IsSkipped;
    public void RefreshStatus(LocalizedText text) => StatusMessage =
        (!IsProcessed ? "○ " : IsSkipped ? "− " : IsSuccessful ? "✓ " : "! ") +
        text[!IsProcessed ? "Status_Pending" : IsSkipped ? "Status_Skipped" : IsSuccessful ? "Status_Success" : "Status_Failed"];
}
