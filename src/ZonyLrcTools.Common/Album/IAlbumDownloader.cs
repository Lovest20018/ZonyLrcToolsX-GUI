namespace ZonyLrcTools.Common.Album;

public interface IAlbumDownloader
{
    Task DownloadAsync(List<MusicInfo> needDownloadMusicInfos,
        int parallelCount = 2,
        CancellationToken cancellationToken = default,
        Func<MusicInfo, Task>? callback = null);

    IEnumerable<IAlbumProvider> AvailableProviders { get; }
}
