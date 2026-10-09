using System.Collections.ObjectModel;
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
using System.Diagnostics;
using System.Text;

namespace ZonyLrcTools.Desktop.ViewModels;

public partial class LyricsDownloadViewModel : ViewModelBase
{
    private readonly ILyricsDownloader _lyricsDownloader;
    private readonly IDialogService _dialogService;
    private readonly IFileScanner _fileScanner;
    private readonly ITagLoader _tagLoader;
    private readonly GlobalOptions _options;
    private readonly IUILocalizationService? _localization;

    private CancellationTokenSource? _downloadCts;
    private Dictionary<string, MusicFileViewModel> _musicFileMap = new();
    private List<MusicInfo> _scannedMusicInfos = new();

    [ObservableProperty]
    private string? _selectedFolderPath;

    [ObservableProperty]
    private int _parallelCount = 2;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private int _skippedCount;

    [ObservableProperty]
    private string _taskStatus = "选择音乐文件夹，扫描后点击开始下载。歌词保存在歌曲旁边。";

    [ObservableProperty]
    private MusicFileViewModel? _selectedMusicFile;

    [ObservableProperty]
    private string _previewText = "选择列表中的歌曲，然后点击“预览歌词”。";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryFailedCommand))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    private bool _isDownloading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartDownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryFailedCommand))]
    [NotifyPropertyChangedFor(nameof(CanStartDownload))]
    private bool _isScanning;

    [ObservableProperty]
    private int _scanProgressCount;

    [ObservableProperty]
    private int _scanTotalCount;

    [ObservableProperty]
    private double _scanProgressPercentage;

    [ObservableProperty]
    private string? _currentProcessingFile;

    // Localized strings
    public virtual string LyricsTitle => _localization?["Lyrics_Title"] ?? "Lyrics Download";
    public virtual string LyricsDescription => _localization?["Lyrics_Description"] ?? "Batch download lyrics for your music files";
    public virtual bool IsLyrics => true;
    public string LyricsFolderLabel => _localization?["Lyrics_FolderLabel"] ?? "Music Folder";
    public string LyricsSelectFolder => _localization?["Lyrics_SelectFolder"] ?? "Select a folder containing music files...";
    public string LyricsBrowse => _localization?["Lyrics_Browse"] ?? "Browse...";
    public string LyricsParallel => _localization?["Lyrics_Parallel"] ?? "Parallel:";
    public string LyricsStartDownload => _localization?["Lyrics_StartDownload"] ?? "Start Download";
    public string LyricsStopDownload => _localization?["Lyrics_StopDownload"] ?? "Stop Download";
    public string LyricsScanning => _localization?["Lyrics_Status_Scanning"] ?? "Scanning files...";
    public string CommonTotal => _localization?["Common_Total"] ?? "Total:";
    public string CommonFiles => _localization?["Common_Files"] ?? "files";
    public string CommonSuccess => _localization?["Common_Success"] ?? "Success:";
    public string CommonFailed => _localization?["Common_Failed"] ?? "Failed:";
    public string ColumnSongName => _localization?["Column_SongName"] ?? "Song Name";
    public string ColumnArtist => _localization?["Column_Artist"] ?? "Artist";
    public string ColumnFilePath => _localization?["Column_FilePath"] ?? "File Path";
    public string ColumnStatus => _localization?["Column_Status"] ?? "Status";

    public bool CanStartDownload => !IsDownloading && !IsScanning && MusicFiles.Count > 0;

    public bool HasNoFiles => MusicFiles.Count == 0;

    public string EmptyStateText => _localization?["Lyrics_EmptyState"] ?? "No tasks yet, please select a folder to start scanning";

    public ObservableCollection<MusicFileViewModel> MusicFiles { get; } = new();

    public LyricsDownloadViewModel(
        ILyricsDownloader lyricsDownloader,
        IDialogService dialogService,
        IFileScanner fileScanner,
        ITagLoader tagLoader,
        IOptions<GlobalOptions> options,
        IUILocalizationService? localization = null)
    {
        _lyricsDownloader = lyricsDownloader;
        _dialogService = dialogService;
        _fileScanner = fileScanner;
        _tagLoader = tagLoader;
        _options = options.Value;
        _localization = localization;

        if (_localization != null)
        {
            _localization.LanguageChanged += OnLanguageChanged;
        }

        MusicFiles.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasNoFiles));
            OnPropertyChanged(nameof(CanStartDownload));
            StartDownloadCommand.NotifyCanExecuteChanged();
            RetryFailedCommand.NotifyCanExecuteChanged();
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(LyricsTitle));
        OnPropertyChanged(nameof(LyricsDescription));
        OnPropertyChanged(nameof(LyricsFolderLabel));
        OnPropertyChanged(nameof(LyricsSelectFolder));
        OnPropertyChanged(nameof(LyricsBrowse));
        OnPropertyChanged(nameof(LyricsParallel));
        OnPropertyChanged(nameof(LyricsStartDownload));
        OnPropertyChanged(nameof(LyricsStopDownload));
        OnPropertyChanged(nameof(LyricsScanning));
        OnPropertyChanged(nameof(CommonTotal));
        OnPropertyChanged(nameof(CommonFiles));
        OnPropertyChanged(nameof(CommonSuccess));
        OnPropertyChanged(nameof(CommonFailed));
        OnPropertyChanged(nameof(ColumnSongName));
        OnPropertyChanged(nameof(ColumnArtist));
        OnPropertyChanged(nameof(ColumnFilePath));
        OnPropertyChanged(nameof(ColumnStatus));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    [RelayCommand(CanExecute = nameof(CanSelectFolder))]
    private async Task SelectFolderAsync()
    {
        var folder = await _dialogService.ShowFolderPickerAsync(
            _localization?["Lyrics_SelectFolder"] ?? "Select Music Folder");
        if (string.IsNullOrEmpty(folder)) return;

        SelectedFolderPath = folder;
        await ScanFilesAsync();
    }

    private bool CanSelectFolder => !IsScanning && !IsDownloading;

    public async Task ScanFolderAsync(string folder)
    {
        if (IsScanning || IsDownloading) return;
        SelectedFolderPath = folder;
        await ScanFilesAsync();
    }

    private async Task ScanFilesAsync()
    {
        if (string.IsNullOrEmpty(SelectedFolderPath)) return;

        IsScanning = true;
        MusicFiles.Clear();
        _musicFileMap.Clear();
        _scannedMusicInfos.Clear();
        ScanProgressCount = 0;
        ScanTotalCount = 0;
        ScanProgressPercentage = 0;
        TotalCount = 0;
        CompletedCount = 0;
        FailedCount = 0;
        SkippedCount = 0;
        ProgressPercentage = 0;
        TaskStatus = "正在扫描音乐文件…";

        try
        {
            var files = (await Task.Run(() => _fileScanner.ScanMusicFilesAsync(SelectedFolderPath, _options.SupportFileExtensions)))
                .ToList();

            if (files.Count == 0)
            {
                await _dialogService.ShowMessageAsync(
                    _localization?["Common_Info"] ?? "Info",
                    _localization?["Error_NoMusicFiles"] ?? "No music files found in the selected folder.");
                IsScanning = false;
                OnPropertyChanged(nameof(CanStartDownload));
                return;
            }

            ScanTotalCount = files.Count;

            using var warpTask = new WarpTask(ParallelCount);
            var scanTasks = files.Select(file =>
                warpTask.RunAsync(async () =>
                {
                    MusicInfo? musicInfo = null;
                    try { musicInfo = await Task.Run(async () => await _tagLoader.LoadTagAsync(file)); }
                    catch (Exception) { /* Other files can still be scanned. */ }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (musicInfo != null &&
                            (!string.IsNullOrEmpty(musicInfo.Name) || !string.IsNullOrEmpty(musicInfo.Artist)))
                        {
                            var vm = new MusicFileViewModel
                            {
                                FilePath = musicInfo.FilePath,
                                Name = musicInfo.Name,
                                Artist = musicInfo.Artist,
                                StatusMessage = _localization?["Status_Pending"] ?? "Pending"
                            };
                            MusicFiles.Add(vm);
                            _musicFileMap[musicInfo.FilePath] = vm;
                            _scannedMusicInfos.Add(musicInfo);
                        }

                        ScanProgressCount++;
                        ScanProgressPercentage = ScanProgressCount * 100.0 / ScanTotalCount;
                    });
                }));

            await Task.WhenAll(scanTasks);

            TotalCount = MusicFiles.Count;
            TaskStatus = $"已扫描 {files.Count} 个文件，可下载 {TotalCount} 首。可直接修改列表中的歌名和歌手。";
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                _localization?["Common_Error"] ?? "Error",
                ex.Message);
        }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(nameof(CanStartDownload));
            StartDownloadCommand.NotifyCanExecuteChanged();
            RetryFailedCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartDownload))]
    private async Task StartDownloadAsync()
    {
        await RunDownloadAsync(_scannedMusicInfos);
    }

    private bool CanRetryFailed => CanStartDownload && MusicFiles.Any(f => f.IsProcessed && !f.IsSuccessful);

    [RelayCommand(CanExecute = nameof(CanRetryFailed))]
    private async Task RetryFailedAsync()
    {
        await RunDownloadAsync(_scannedMusicInfos.Where(i =>
            _musicFileMap[i.FilePath] is { IsProcessed: true, IsSuccessful: false }).ToList());
    }

    protected virtual Task DownloadCoreAsync(List<MusicInfo> infos, int parallelCount,
        Func<MusicInfo, Task> callback, CancellationToken cancellationToken) =>
        _lyricsDownloader.DownloadAsync(infos, parallelCount, callback, cancellationToken);

    private async Task RunDownloadAsync(List<MusicInfo> infos)
    {
        if (infos.Count == 0 || IsScanning || IsDownloading) return;
        if (!_options.Provider.Lyric.Plugin.Any(p => p.Priority >= 0) && IsLyrics)
        {
            await _dialogService.ShowMessageAsync("无法下载", "请先在设置中启用至少一个歌词源。");
            return;
        }

        IsDownloading = true;
        CompletedCount = 0;
        FailedCount = 0;
        SkippedCount = 0;
        ProgressPercentage = 0;
        TotalCount = infos.Count;
        TaskStatus = "正在下载…";

        // Reset all row statuses
        foreach (var info in infos)
        {
            var vm = _musicFileMap[info.FilePath];
            info.Name = vm.Name.Trim();
            info.Artist = vm.Artist.Trim();
            vm.IsProcessed = false;
            vm.IsSuccessful = false;
            vm.StatusMessage = _localization?["Status_Pending"] ?? "Pending";
        }

        _downloadCts = new CancellationTokenSource();

        try
        {
            await DownloadCoreAsync(
                infos,
                ParallelCount,
                async musicInfo =>
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_musicFileMap.TryGetValue(musicInfo.FilePath, out var vm))
                        {
                            vm.IsProcessed = true;
                            vm.IsSuccessful = musicInfo.IsSuccessful;

                            if (musicInfo.IsPruneMusic || musicInfo.IsSkipped)
                            {
                                vm.StatusMessage = _localization?["Status_Skipped"] ?? "Skipped";
                            }
                            else if (musicInfo.IsSuccessful)
                            {
                                vm.StatusMessage = _localization?["Status_Success"] ?? "Success";
                            }
                            else
                            {
                                vm.StatusMessage = _localization?["Status_Failed"] ?? "Failed";
                            }
                        }

                        if (musicInfo.IsPruneMusic || musicInfo.IsSkipped)
                            SkippedCount++;
                        else if (musicInfo.IsSuccessful)
                            CompletedCount++;
                        else
                            FailedCount++;

                        ProgressPercentage = (CompletedCount + FailedCount + SkippedCount) * 100.0 / TotalCount;
                    });
                },
                _downloadCts.Token);
            TaskStatus = $"下载完成：成功 {CompletedCount}，跳过 {SkippedCount}，失败 {FailedCount}。";
        }
        catch (OperationCanceledException)
        {
            TaskStatus = "已停止。尚未完成的歌曲可再次开始下载。";
        }
        catch (Exception ex)
        {
            TaskStatus = $"下载出错：{ex.Message}";
            await _dialogService.ShowMessageAsync("下载失败", ex.Message);
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
            RetryFailedCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task PreviewLyricsAsync()
    {
        if (SelectedMusicFile == null) { PreviewText = "请先在列表中选择一首歌曲。"; return; }
        var path = Path.ChangeExtension(SelectedMusicFile.FilePath, ".lrc");
        try
        {
            if (!File.Exists(path)) { PreviewText = "这首歌曲还没有 .lrc 文件，请先下载。"; return; }
            var encoding = _options.Provider.Lyric.Config.FileEncoding;
            using var reader = new StreamReader(path, Encoding.GetEncoding(encoding == "utf-8-bom" ? "utf-8" : encoding), true);
            PreviewText = await reader.ReadToEndAsync();
        }
        catch (Exception ex) { PreviewText = $"无法读取歌词：{ex.Message}"; }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (Directory.Exists(SelectedFolderPath))
            Process.Start(new ProcessStartInfo("explorer.exe", SelectedFolderPath!) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task ExportFailedAsync()
    {
        var failed = MusicFiles.Where(f => f.IsProcessed && !f.IsSuccessful).ToList();
        if (failed.Count == 0) { await _dialogService.ShowMessageAsync("失败列表", "当前没有下载失败的歌曲。"); return; }
        var folder = await _dialogService.ShowFolderPickerAsync("选择失败列表保存目录");
        if (folder == null) return;
        static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
        try
        {
            var path = Path.Combine(folder, $"歌词下载失败_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            await File.WriteAllLinesAsync(path, new[] { "Song,Artist,FilePath" }
                .Concat(failed.Select(f => $"{Csv(f.Name)},{Csv(f.Artist)},{Csv(f.FilePath)}")), new UTF8Encoding(true));
            TaskStatus = $"失败列表已保存：{path}";
        }
        catch (Exception ex) { await _dialogService.ShowMessageAsync("导出失败", ex.Message); }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        _downloadCts?.Cancel();
    }
}

public partial class MusicFileViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private bool _isProcessed;

    [ObservableProperty]
    private bool _isSuccessful;

    [ObservableProperty]
    private string? _statusMessage;
}
