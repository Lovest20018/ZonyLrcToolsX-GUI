using Microsoft.Extensions.Options;
using ZonyLrcTools.Common;
using ZonyLrcTools.Common.Album;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.IO;
using ZonyLrcTools.Common.Lyrics;
using ZonyLrcTools.Common.TagInfo;
using ZonyLrcTools.Desktop.Infrastructure.Localization;
using ZonyLrcTools.Desktop.Services;

namespace ZonyLrcTools.Desktop.ViewModels;

public class AlbumDownloadViewModel : LyricsDownloadViewModel
{
    private readonly IAlbumDownloader _albumDownloader;
    public override bool IsLyrics => false;

    public AlbumDownloadViewModel(ILyricsDownloader lyricsDownloader, IAlbumDownloader albumDownloader,
        IDialogService dialogs, IFileScanner scanner, ITagLoader tags, IOptions<GlobalOptions> options,
        IUILocalizationService localization)
        : base(lyricsDownloader, dialogs, scanner, tags, options, localization)
    {
        _albumDownloader = albumDownloader;
    }

    protected override Task DownloadCoreAsync(List<MusicInfo> infos, int parallelCount,
        Func<MusicInfo, Task> callback, CancellationToken cancellationToken) =>
        _albumDownloader.DownloadAsync(infos, parallelCount, cancellationToken, callback);
}
