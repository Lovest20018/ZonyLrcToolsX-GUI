using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Options;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.DependencyInject;
using ZonyLrcTools.Common.Infrastructure.Exceptions;
using ZonyLrcTools.Common.Infrastructure.Extensions;
using ZonyLrcTools.Common.Infrastructure.Logging;
using ZonyLrcTools.Common.Infrastructure.Threading;

namespace ZonyLrcTools.Common.Lyrics;

/// <inheritdoc cref="ZonyLrcTools.Common.Lyrics.ILyricsDownloader" />
public class LyricsDownloader : ILyricsDownloader, ISingletonDependency
{
    private readonly IEnumerable<ILyricsProvider> _lyricsProviders;
    private readonly IWarpLogger _logger;
    private readonly GlobalOptions _options;

    public IEnumerable<ILyricsProvider> AvailableProviders => new Lazy<IEnumerable<ILyricsProvider>>(() =>
    {
        return _options.Provider.Lyric.Plugin
            .Where(op => op.Priority != -1)
            .OrderBy(op => op.Priority)
            .Join(_lyricsProviders,
                op => op.Name,
                loader => loader.DownloaderName,
                (_, loader) => loader);
    }).Value;

    public LyricsDownloader(IEnumerable<ILyricsProvider> lyricsProviders,
        IOptions<GlobalOptions> options,
        IWarpLogger logger)
    {
        _lyricsProviders = lyricsProviders;
        _logger = logger;
        _options = options.Value;
    }

    public async Task DownloadAsync(List<MusicInfo> needDownloadMusicInfos,
        int parallelCount = 1,
        Func<MusicInfo, Task>? callback = null,
        CancellationToken cancellationToken = default)
    {
        await _logger.InfoAsync("开始下载歌词文件数据...");

        if (parallelCount <= 0)
        {
            parallelCount = 1;
        }

        using var warpTask = new WarpTask(parallelCount);
        var downloadTasks = needDownloadMusicInfos.Select(info =>
            warpTask.RunAsync(() =>
                Task.Run(async () =>
                {
                    info.IsSuccessful = false;
                    info.IsPruneMusic = false;
                    info.IsSkipped = false;
                    cancellationToken.ThrowIfCancellationRequested();
                    var destination = Path.ChangeExtension(info.FilePath, ".lrc");
                    if (_options.Provider.Lyric.Config.IsSkipExistLyricFiles && File.Exists(destination))
                    {
                        info.IsSuccessful = true;
                        info.IsSkipped = true;
                        if (callback != null) await callback(info);
                        return;
                    }
                    // Try to download lyrics from all available providers.
                    foreach (var lyricsProvider in AvailableProviders)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await DownloadAndWriteLyricsAsync(lyricsProvider, info, cancellationToken);

                        if (!info.IsSuccessful) continue;
                        _logger.LogSuccessful(info);
                        break;
                    }

                    if (callback != null) await callback(info);
                }, cancellationToken), cancellationToken));

        await Task.WhenAll(downloadTasks);

        var successfulCount = needDownloadMusicInfos.Count(m => m is { IsSuccessful: true, IsPruneMusic: false });
        var skippedCount = needDownloadMusicInfos.Count(m => m is { IsSuccessful: true, IsPruneMusic: true });
        var failedCount = needDownloadMusicInfos.Count(m => m.IsSuccessful == false);
        await _logger.InfoAsync($"歌词数据下载完成，成功: {successfulCount} 跳过(纯音乐): {skippedCount} 失败{failedCount}。");
        try
        {
            await LogFailedSongFilesInfo(Path.Combine(AppContext.BaseDirectory,
                $"歌词下载失败列表_{DateTime.Now:yyyyMMddHHmmss}.txt"), needDownloadMusicInfos);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await _logger.WarnAsync($"无法自动保存失败列表，可在界面中选择其他目录导出：{ex.Message}");
        }
    }

    private async Task DownloadAndWriteLyricsAsync(ILyricsProvider provider, MusicInfo info, CancellationToken cancellationToken)
    {
        try
        {
            var lyrics = await provider.DownloadAsync(info.Name, info.Artist, info.TotalTime)
                .AsTask().WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (lyrics.IsPruneMusic)
            {
                info.IsSuccessful = true;
                info.IsPruneMusic = true;
                return;
            }

            var newLyricsFilePath = Path.Combine(Path.GetDirectoryName(info.FilePath)!,
                $"{Path.GetFileNameWithoutExtension(info.FilePath)}.lrc");

            // Complete the replacement only after a successful write.
            var temporaryPath = newLyricsFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, Utf8ToSelectedEncoding(lyrics), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, newLyricsFilePath, true);
            }
            finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }

            info.IsSuccessful = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ErrorCodeException ex)
        {
            _logger.LogWarningInfo(ex);
            info.IsSuccessful = false;
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync($"下载歌词文件时发生错误：{ex.Message}，歌曲名: {info.Name}，歌手: {info.Artist}。");
            info.IsSuccessful = false;
        }
    }

    // Convert UTF-8 to selected encoding.
    private byte[] Utf8ToSelectedEncoding(LyricsItemCollection lyrics)
    {
        // If the encoding is UTF-8 BOM, add the BOM to the front of the lyrics.
        if (_options.Provider.Lyric.Config.FileEncoding == "utf-8-bom")
        {
            return Encoding.UTF8.GetPreamble().Concat(lyrics.GetUtf8Bytes()).ToArray();
        }

        try
        {
            return Encoding.Convert(Encoding.UTF8,
                Encoding.GetEncoding(_options.Provider.Lyric.Config.FileEncoding), lyrics.GetUtf8Bytes());
        }
        catch (ArgumentException) { throw new ErrorCodeException(ErrorCodes.NotSupportedFileEncoding); }
    }

    private async Task LogFailedSongFilesInfo(string outFilePath, IEnumerable<MusicInfo> musicInfos)
    {
        var failedSongList = musicInfos.Where(m => m.IsSuccessful == false).ToImmutableList();
        if (!failedSongList.Any())
        {
            return;
        }

        var failedSongFiles = new StringBuilder();
        failedSongFiles.AppendLine("歌曲名,歌手,路径");

        foreach (var failedSongFile in failedSongList)
        {
            failedSongFiles.AppendLine($"{failedSongFile.Name},{failedSongFile.Artist},{failedSongFile.FilePath}");
        }

        await File.WriteAllTextAsync(outFilePath, failedSongFiles.ToString());
    }
}
