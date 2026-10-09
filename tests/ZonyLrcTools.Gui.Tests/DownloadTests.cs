using System.Text;
using Microsoft.Extensions.Options;
using Xunit;
using ZonyLrcTools.Common;
using ZonyLrcTools.Common.Album;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.Logging;
using ZonyLrcTools.Common.Lyrics;

namespace ZonyLrcTools.Gui.Tests;

public sealed class DownloadTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "zony-gui-tests-" + Guid.NewGuid());
    private readonly GlobalOptions _options = new()
    {
        Provider = new ProviderOptions
        {
            Lyric = new LyricsOptions
            {
                Config = new GlobalLyricsConfigOptions(),
                Plugin = [new LyricsProviderOptions { Name = "Test", Priority = 1 }]
            }
        }
    };

    public DownloadTests()
    {
        Directory.CreateDirectory(_folder);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private MusicInfo Song() => new(Path.Combine(_folder, "Artist - Song.mp3"), "Song", "Artist");
    private LyricsDownloader Downloader(FakeLyricsProvider provider) =>
        new([provider], Options.Create(_options), new NullLogger());

    [Fact]
    public async Task SkipExistingDoesNotRequestProviderOrModifyFile()
    {
        _options.Provider.Lyric.Config.IsSkipExistLyricFiles = true;
        var provider = new FakeLyricsProvider(_options.Provider.Lyric.Config);
        var song = Song();
        var path = Path.ChangeExtension(song.FilePath, ".lrc");
        await File.WriteAllTextAsync(path, "original");
        var callbacks = 0;
        await Downloader(provider).DownloadAsync([song], callback: _ => { callbacks++; return Task.CompletedTask; });
        Assert.Equal(0, provider.Calls);
        Assert.Equal("original", await File.ReadAllTextAsync(path));
        Assert.True(song.IsSkipped);
        Assert.True(song.IsSuccessful);
        Assert.Equal(1, callbacks);
    }

    [Fact]
    public async Task ProviderFailurePreservesExistingLyrics()
    {
        var song = Song();
        var path = Path.ChangeExtension(song.FilePath, ".lrc");
        await File.WriteAllTextAsync(path, "original");
        var provider = new FakeLyricsProvider(_options.Provider.Lyric.Config) { Fail = true };
        await Downloader(provider).DownloadAsync([song]);
        Assert.Equal("original", await File.ReadAllTextAsync(path));
        Assert.False(song.IsSuccessful);
    }

    [Fact]
    public async Task NoEnabledProvidersDoesNotReportStaleSuccess()
    {
        _options.Provider.Lyric.Plugin.First().Priority = -1;
        var song = Song();
        await Downloader(new(_options.Provider.Lyric.Config)).DownloadAsync([song]);
        Assert.False(song.IsSuccessful);
        Assert.False(File.Exists(Path.ChangeExtension(song.FilePath, ".lrc")));
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-8-bom")]
    [InlineData("gbk")]
    [InlineData("gb2312")]
    public async Task WritesSelectedEncodingAndReplacesCompleteFile(string encoding)
    {
        _options.Provider.Lyric.Config.FileEncoding = encoding;
        var song = Song();
        await Downloader(new(_options.Provider.Lyric.Config)).DownloadAsync([song]);
        Assert.True(song.IsSuccessful);
        var bytes = await File.ReadAllBytesAsync(Path.ChangeExtension(song.FilePath, ".lrc"));
        Assert.Equal(encoding == "utf-8-bom", bytes.Take(3).SequenceEqual(new byte[] { 239, 187, 191 }));
        using var reader = new StreamReader(Path.ChangeExtension(song.FilePath, ".lrc"),
            Encoding.GetEncoding(encoding == "utf-8-bom" ? "utf-8" : encoding), true);
        Assert.Contains("中文歌词", await reader.ReadToEndAsync());
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public async Task CancellingInFlightRequestDoesNotWriteLateResult()
    {
        var song = Song();
        var provider = new FakeLyricsProvider(_options.Provider.Lyric.Config) { Delay = true };
        using var cts = new CancellationTokenSource();
        var work = Downloader(provider).DownloadAsync([song], cancellationToken: cts.Token);
        await provider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work);
        provider.Release.TrySetResult();
        Assert.False(File.Exists(Path.ChangeExtension(song.FilePath, ".lrc")));
    }

    [Fact]
    public async Task AlbumReportsSuccessAndSkipsExistingOnSecondRun()
    {
        var provider = new FakeAlbumProvider();
        var downloader = new AlbumDownloader([provider], new NullLogger());
        var song = Song();
        var callbacks = 0;
        await downloader.DownloadAsync([song], callback: _ => { callbacks++; return Task.CompletedTask; });
        Assert.True(song.IsSuccessful);
        Assert.True(File.Exists(Path.ChangeExtension(song.FilePath, ".png")));
        await downloader.DownloadAsync([song]);
        Assert.True(song.IsSkipped);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(1, callbacks);
    }

    public void Dispose() => Directory.Delete(_folder, true);

    private sealed class FakeLyricsProvider(GlobalLyricsConfigOptions options) : ILyricsProvider
    {
        public string DownloaderName => "Test";
        public int Calls { get; private set; }
        public bool Fail { get; init; }
        public bool Delay { get; init; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async ValueTask<LyricsItemCollection> DownloadAsync(string songName, string artist, long? duration = null)
        {
            Calls++;
            Started.TrySetResult();
            if (Delay) await Release.Task;
            if (Fail) throw new IOException("provider unavailable");
            return new LyricsItemCollection(options) { new LyricsItem("[00:01.00]中文歌词") };
        }
    }

    private sealed class FakeAlbumProvider : IAlbumProvider
    {
        public string DownloaderName => InternalAlbumProviderNames.NetEase;
        public int Calls { get; private set; }
        public ValueTask<byte[]> DownloadAsync(string songName, string artist)
        {
            Calls++;
            return ValueTask.FromResult(new byte[] { 137, 80, 78, 71 });
        }
    }

    internal sealed class NullLogger : IWarpLogger
    {
        public Task DebugAsync(string message, Exception? exception = null) => Task.CompletedTask;
        public Task InfoAsync(string message, Exception? exception = null) => Task.CompletedTask;
        public Task WarnAsync(string message, Exception? exception = null) => Task.CompletedTask;
        public Task ErrorAsync(string message, Exception? exception = null) => Task.CompletedTask;
    }
}
