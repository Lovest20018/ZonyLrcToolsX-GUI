using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Lyrics;
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
    private string _matchedName = "";
    private string _matchedArtist = "";

    [ObservableProperty] private string _songName = "";
    [ObservableProperty] private string _artist = "";
    [ObservableProperty] private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    [ObservableProperty] private string _preview = "";
    [ObservableProperty] private string _status = "输入歌名和歌手，匹配后可预览并保存歌词。";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MatchCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasLyrics;

    public ManualDownloadViewModel(ILyricsDownloader downloader, IDialogService dialogs,
        AvaloniaWarpLogger logger, IOptions<GlobalOptions> options)
    {
        _downloader = downloader;
        _dialogs = dialogs;
        _logger = logger;
        _options = options.Value;
    }

    private bool CanMatch => !IsBusy;
    private bool CanSave => !IsBusy && HasLyrics;

    [RelayCommand(CanExecute = nameof(CanMatch))]
    private async Task MatchAsync()
    {
        if (string.IsNullOrWhiteSpace(SongName) || string.IsNullOrWhiteSpace(Artist))
        {
            Status = "请填写歌名和歌手，以提高匹配准确度。";
            return;
        }
        IsBusy = true;
        HasLyrics = false;
        Preview = "";
        _lyrics = null;
        _cts = new CancellationTokenSource();
        _matchedName = SongName.Trim();
        _matchedArtist = Artist.Trim();
        try
        {
            var providers = _downloader.AvailableProviders.ToList();
            if (providers.Count == 0) { Status = "请在设置中启用至少一个歌词源。"; return; }
            foreach (var provider in providers)
            {
                _cts.Token.ThrowIfCancellationRequested();
                Status = $"正在从 {provider.DownloaderName} 匹配…";
                try
                {
                    var lyrics = await provider.DownloadAsync(_matchedName, _matchedArtist)
                        .AsTask().WaitAsync(_cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();
                    if (lyrics.IsPruneMusic)
                    {
                        Status = $"{provider.DownloaderName} 没有返回歌词，继续尝试其他源…";
                        continue;
                    }
                    _lyrics = lyrics;
                    Preview = lyrics.ToString();
                    HasLyrics = true;
                    Status = $"匹配完成 · {provider.DownloaderName} · {lyrics.Count} 行。请检查歌词后保存。";
                    return;
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    await _logger.WarnAsync($"{provider.DownloaderName} 匹配失败：{ex.Message}");
                }
            }
            Status = "没有匹配到歌词。请检查歌名、歌手，或调整歌词源。";
        }
        catch (OperationCanceledException) { Status = "已取消匹配。"; }
        catch (Exception ex) { Status = $"匹配失败：{ex.Message}"; }
        finally { IsBusy = false; _cts.Dispose(); _cts = null; }
    }

    [RelayCommand] private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var folder = await _dialogs.ShowFolderPickerAsync("选择歌词保存目录");
        if (folder != null) OutputFolder = folder;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_lyrics == null || string.IsNullOrWhiteSpace(OutputFolder)) return;
        try
        {
            Directory.CreateDirectory(OutputFolder);
            var name = string.Concat($"{_matchedArtist} - {_matchedName}"
                .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).TrimEnd('.', ' ');
            var path = Path.Combine(OutputFolder, name + ".lrc");
            if (File.Exists(path) && !await _dialogs.ShowConfirmDialogAsync("覆盖歌词", $"文件已存在，是否覆盖？\n{path}"))
                return;
            var encoding = _options.Provider.Lyric.Config.FileEncoding;
            var selected = encoding == "utf-8-bom" ? new UTF8Encoding(true) :
                encoding == "utf-8" ? new UTF8Encoding(false) : Encoding.GetEncoding(encoding);
            var bytes = selected.GetPreamble().Concat(selected.GetBytes(_lyrics.ToString())).ToArray();
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporary, bytes);
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            Status = $"已保存：{path}";
        }
        catch (Exception ex) { Status = $"保存失败：{ex.Message}"; }
    }
}
