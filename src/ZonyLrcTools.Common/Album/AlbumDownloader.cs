using ZonyLrcTools.Common.Infrastructure.DependencyInject;
using ZonyLrcTools.Common.Infrastructure.Exceptions;
using ZonyLrcTools.Common.Infrastructure.Extensions;
using ZonyLrcTools.Common.Infrastructure.IO;
using ZonyLrcTools.Common.Infrastructure.Logging;
using ZonyLrcTools.Common.Infrastructure.Threading;

namespace ZonyLrcTools.Common.Album;

public class AlbumDownloader : IAlbumDownloader, ISingletonDependency
{
    private readonly IEnumerable<IAlbumProvider> _albumProviders;

    public IEnumerable<IAlbumProvider> AvailableProviders => new Lazy<IEnumerable<IAlbumProvider>>(() =>
    {
        return _albumProviders.Where(d => d.DownloaderName == InternalAlbumProviderNames.NetEase);
    }).Value;

    private readonly IWarpLogger _logger;

    public AlbumDownloader(IEnumerable<IAlbumProvider> albumProviders,
        IWarpLogger logger)
    {
        _albumProviders = albumProviders;
        _logger = logger;
    }

    public async Task DownloadAsync(List<MusicInfo> needDownloadMusicInfos,
        int parallelCount = 2,
        CancellationToken cancellationToken = default,
        Func<MusicInfo, Task>? callback = null)
    {
        await _logger.InfoAsync("开始下载专辑图像数据...");

        var provider = AvailableProviders.FirstOrDefault(d => d.DownloaderName == InternalAlbumProviderNames.NetEase);
        if (provider == null)
        {
            return;
        }

        using var warpTask = new WarpTask(Math.Max(1, parallelCount));
        var warpTaskList = needDownloadMusicInfos.Select(info =>
            warpTask.RunAsync(() =>
                Task.Run(async () =>
                {
                    info.IsSuccessful = false;
                    info.IsSkipped = false;
                    info.IsPruneMusic = false;
                    try
                    {
                        var filePath = Path.Combine(Path.GetDirectoryName(info.FilePath)!, $"{Path.GetFileNameWithoutExtension(info.FilePath)}.png");
                        if (File.Exists(filePath))
                        {
                            info.IsSkipped = true;
                            info.IsSuccessful = true;
                            if (callback != null) await callback(info);
                            return;
                        }
                        var album = await provider.DownloadAsync(info.Name, info.Artist)
                            .AsTask().WaitAsync(cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (album.Length > 0)
                        {
                            var temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                            try
                            {
                                await File.WriteAllBytesAsync(temporary, album, cancellationToken);
                                cancellationToken.ThrowIfCancellationRequested();
                                File.Move(temporary, filePath, false);
                            }
                            finally { if (File.Exists(temporary)) File.Delete(temporary); }
                            info.IsSuccessful = true;
                            _logger.LogSuccessful(info);
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (ErrorCodeException ex)
                    {
                        _logger.LogWarningInfo(ex);
                    }
                    catch (Exception ex)
                    {
                        await _logger.ErrorAsync($"下载封面失败：{info.Name}，{ex.Message}");
                    }
                    if (callback != null) await callback(info);
                }, cancellationToken), cancellationToken));

        await Task.WhenAll(warpTaskList);

        await _logger.InfoAsync($"专辑数据下载完成，成功: {needDownloadMusicInfos.Count(m => m.IsSuccessful)} 失败{needDownloadMusicInfos.Count(m => m.IsSuccessful == false)}。");
    }
}
