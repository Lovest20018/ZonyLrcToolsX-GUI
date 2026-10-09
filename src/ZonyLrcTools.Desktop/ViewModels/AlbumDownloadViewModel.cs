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
    public override string LyricsTitle => "专辑封面";
    public override string LyricsDescription => "从网易云音乐匹配封面，保存为歌曲旁的 .png 文件；已有封面自动跳过。";
    public override bool IsLyrics => false;

    public AlbumDownloadViewModel(ILyricsDownloader lyricsDownloader, IAlbumDownloader albumDownloader,
        IDialogService dialogs, IFileScanner scanner, ITagLoader tags, IOptions<GlobalOptions> options,
        IUILocalizationService localization)
        : base(lyricsDownloader, dialogs, scanner, tags, options, localization)
    {
        _albumDownloader = albumDownloader;
        TaskStatus = "选择音乐文件夹，扫描后开始下载专辑封面。";
    }

    protected override Task DownloadCoreAsync(List<MusicInfo> infos, int parallelCount,
        Func<MusicInfo, Task> callback, CancellationToken cancellationToken) =>
        _albumDownloader.DownloadAsync(infos, parallelCount, cancellationToken, callback);
}
