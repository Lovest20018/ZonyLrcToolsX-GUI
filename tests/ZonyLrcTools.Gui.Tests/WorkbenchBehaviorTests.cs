using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.Options;
using ZonyLrcTools.Common;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.IO;
using ZonyLrcTools.Common.Lyrics;
using ZonyLrcTools.Common.TagInfo;
using ZonyLrcTools.Desktop.Services;
using ZonyLrcTools.Desktop.ViewModels;
using Xunit;

namespace ZonyLrcTools.Gui.Tests;

public partial class UiTests
{
    [AvaloniaFact]
    public void OpenGridHeadersFollowLanguageChanges()
    {
        var localization = ZonyLrcTools.Desktop.App.Services!.GetRequiredService<ZonyLrcTools.Desktop.Infrastructure.Localization.IUILocalizationService>();
        localization.SetLanguage("zh-CN");
        var page = new ZonyLrcTools.Desktop.Views.Pages.LyricsDownloadPage { DataContext = Batch(new FixtureDownloader()) };
        var window = new Avalonia.Controls.Window { Content = page };
        window.Show();
        try
        {
            localization.SetLanguage("en-US");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Song Name", new ZonyLrcTools.Desktop.Infrastructure.Localization.LocalizedText()["Column_SongName"]);
            var grid = page.FindControl<Avalonia.Controls.DataGrid>("SongsGrid")!;
            Assert.Equal("Song Name", grid.Columns[0].Header);
        }
        finally { window.Close(); localization.SetLanguage("zh-CN"); }
    }

    [AvaloniaFact]
    public void TranslationOffRetainsMergePreferenceAndSaveErrorsSurviveLanguageChange()
    {
        var localization = ZonyLrcTools.Desktop.App.Services!.GetRequiredService<ZonyLrcTools.Desktop.Infrastructure.Localization.IUILocalizationService>();
        var settings = ZonyLrcTools.Desktop.App.Services!.GetRequiredService<SettingsViewModel>();
        settings.IsTranslationEnabled = true;
        settings.IsOneLineMode = true;
        settings.IsTranslationEnabled = false;
        Assert.True(settings.IsOneLineMode);
        var config = DesktopPreferences.ConfigPath;
        using (var locked = File.Open(config + ".tmp", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            settings.ProxyPort = settings.ProxyPort == 7890 ? 7891 : 7890;
            Assert.True(settings.HasSaveError);
            localization.SetLanguage("en-US");
            Assert.True(settings.HasSaveError);
            Assert.Contains("failed", settings.SaveStatus);
        }
        settings.IsOneLineMode = false;
        Assert.False(settings.HasSaveError);
        localization.SetLanguage("zh-CN");
    }

    [AvaloniaFact]
    public async Task ConcurrencyEditableBeforeScanAndRunningGridStaysSelectable()
    {
        var downloader = new FixtureDownloader { WaitForCancellation = true };
        var model = Batch(downloader, ["song.mp3"]);
        var page = new ZonyLrcTools.Desktop.Views.Pages.LyricsDownloadPage { DataContext = model };
        var window = new Window { Content = page };
        window.Show();
        try
        {
            var input = page.FindControl<NumericUpDown>("ConcurrencyInput")!;
            Assert.True(input.IsEnabled);
            model.ParallelCount = 4;
            await model.ScanFolderAsync("fixture");
            var running = model.StartDownloadCommand.ExecuteAsync(null);
            await downloader.BatchStarted.Task;
            Dispatcher.UIThread.RunJobs();
            Assert.False(input.IsEnabled);
            var grid = page.FindControl<DataGrid>("SongsGrid")!;
            Assert.True(grid.IsEnabled);
            Assert.True(grid.IsReadOnly);
            grid.SelectedItem = model.MusicFiles[0];
            Assert.Same(model.MusicFiles[0], model.SelectedMusicFile);
            model.CancelDownloadCommand.Execute(null);
            Assert.True(model.IsStopping);
            Assert.False(model.CancelDownloadCommand.CanExecute(null));
            await running;
            Assert.Equal(4, downloader.LastParallelCount);
            Assert.True(model.CanConfigure);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task SelectedPreviewRefreshesAfterDownloadAndEscClosesNarrowDrawer()
    {
        var folder = Path.Combine(Path.GetTempPath(), "zony-refresh-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        var model = Batch(new FixtureDownloader { WriteLocalLyrics = true }, [Path.Combine(folder, "success.mp3")]);
        var window = new Window { Width = 690, Height = 560 };
        try
        {
            await model.ScanFolderAsync(folder);
            var page = new ZonyLrcTools.Desktop.Views.Pages.LyricsDownloadPage { DataContext = model };
            window.Content = page;
            window.Show();
            model.SelectedMusicFile = model.MusicFiles[0];
            await model.StartDownloadCommand.ExecuteAsync(null);
            await PumpUntil(() => model.PreviewText.Contains("Downloaded lyrics"));
            Assert.Contains("Downloaded lyrics", model.PreviewText);
            Assert.True(model.IsPreviewOpen);
            page.FindControl<DataGrid>("SongsGrid")!.Focus();
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.False(model.IsPreviewOpen);
            Assert.Single(model.MusicFiles);
        }
        finally { window.Close(); Directory.Delete(folder, true); }
    }

    [AvaloniaFact]
    public async Task EmptyScanAndAllFailedRunHaveConsistentCounts()
    {
        var empty = Batch(new FixtureDownloader());
        await empty.ScanFolderAsync("fixture");
        Assert.True(empty.HasScanned);
        Assert.Equal(0, empty.ScanProgressCount);
        Assert.True(empty.HasNoFiles);
        Assert.False(empty.StartDownloadCommand.CanExecute(null));
        var model = Batch(new FixtureDownloader(), ["a.mp3", "b.mp3"]);
        await model.ScanFolderAsync("fixture");
        await model.StartDownloadCommand.ExecuteAsync(null);
        Assert.Equal(2, model.FailedCount);
        Assert.Equal(2, model.ProcessedCount);
        Assert.Equal(0, model.UnfinishedCount);
        Assert.True(model.RetryFailedCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void SingleNoResultOffersSourceSettingsNavigation()
    {
        var model = new ManualDownloadViewModel(new FixtureDownloader(), new Dialogs(), new AvaloniaWarpLogger(),
            Options.Create(OptionsValue()));
        var page = new ZonyLrcTools.Desktop.Views.Pages.ManualDownloadPage { DataContext = model };
        var window = new Window { Content = page };
        window.Show();
        try
        {
            var button = page.FindControl<Button>("SourceSettingsButton");
            Assert.NotNull(button);
            Assert.True(button.IsVisible);
            button.Command!.Execute(null);
            Assert.Equal(5, ZonyLrcTools.Desktop.App.Services!.GetRequiredService<MainWindowViewModel>().SelectedNavigationIndex);
        }
        finally { window.Close(); }
    }

    private static GlobalOptions OptionsValue() => new()
    {
        SupportFileExtensions = [".mp3"],
        Provider = new ProviderOptions
        {
            Lyric = new LyricsOptions
            {
                Config = new GlobalLyricsConfigOptions(),
                Plugin = [new LyricsProviderOptions { Name = "Fixture", Priority = 1 }]
            }
        }
    };

    [AvaloniaFact]
    public async Task ChangedInputInvalidatesMatchedLyricsAndPreventsSave()
    {
        var downloader = new FixtureDownloader();
        var model = new ManualDownloadViewModel(downloader, new Dialogs(), new AvaloniaWarpLogger(),
            Options.Create(OptionsValue())) { SongName = "Song", Artist = "Artist" };
        await model.MatchCommand.ExecuteAsync(null);
        Assert.True(model.HasLyrics);
        model.SongName = "Another song";
        Assert.False(model.HasLyrics);
        Assert.Empty(model.Preview);
        Assert.False(model.SaveCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task ChangedInputDuringMatchDiscardsLateProviderResult()
    {
        var provider = new FixtureProvider { Wait = true };
        var model = new ManualDownloadViewModel(new FixtureDownloader(provider), new Dialogs(),
            new AvaloniaWarpLogger(), Options.Create(OptionsValue())) { SongName = "Song", Artist = "Artist" };
        var matching = model.MatchCommand.ExecuteAsync(null);
        await provider.Started.Task;
        model.Artist = "Another artist";
        provider.Release.TrySetResult();
        await matching;
        Assert.False(model.HasLyrics);
        Assert.Empty(model.Preview);
    }

    [AvaloniaFact]
    public async Task ChangedInputWhileOverwriteConfirmationWaitsDoesNotWriteOldLyrics()
    {
        var folder = Path.Combine(Path.GetTempPath(), "zony-confirm-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "Artist - Song.lrc");
            await File.WriteAllTextAsync(path, "original");
            var dialogs = new Dialogs { WaitForConfirmation = true };
            var model = new ManualDownloadViewModel(new FixtureDownloader(), dialogs, new AvaloniaWarpLogger(),
                Options.Create(OptionsValue())) { SongName = "Song", Artist = "Artist", OutputFolder = folder };
            await model.MatchCommand.ExecuteAsync(null);
            var saving = model.SaveCommand.ExecuteAsync(null);
            await dialogs.ConfirmationStarted.Task;
            model.SongName = "Another";
            dialogs.Confirm.TrySetResult(true);
            await saving;
            Assert.Equal("original", await File.ReadAllTextAsync(path));
            Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
        }
        finally { Directory.Delete(folder, true); }
    }

    [AvaloniaFact]
    public async Task SelectingRowAutomaticallyLoadsLocalLyricsWithoutProviderCalls()
    {
        var folder = Path.Combine(Path.GetTempPath(), "zony-preview-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "song.mp3");
            await File.WriteAllTextAsync(Path.ChangeExtension(path, ".lrc"), "[00:01.00]Local lyrics");
            var downloader = new FixtureDownloader();
            var model = Batch(downloader);
            model.SelectedMusicFile = new MusicFileViewModel { FilePath = path, Name = "Song" };
            await PumpUntil(() => model.PreviewText.Contains("Local lyrics"));
            Assert.Contains("Local lyrics", model.PreviewText);
            Assert.Equal(0, downloader.Provider.Calls);
        }
        finally { Directory.Delete(folder, true); }
    }

    [AvaloniaFact]
    public async Task RapidSelectionCannotReplaceLatestPreviewWithOlderFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "zony-race-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var first = Path.Combine(folder, "first.mp3");
            var second = Path.Combine(folder, "second.mp3");
            await File.WriteAllTextAsync(Path.ChangeExtension(first, ".lrc"), new string('a', 1_000_000) + "OLD");
            await File.WriteAllTextAsync(Path.ChangeExtension(second, ".lrc"), "[00:01.00]LATEST");
            var model = Batch(new FixtureDownloader());
            model.SelectedMusicFile = new() { FilePath = first };
            model.SelectedMusicFile = new() { FilePath = second };
            await PumpUntil(() => model.PreviewText.Contains("LATEST"));
            await Task.Delay(30);
            Assert.Contains("LATEST", model.PreviewText);
            Assert.DoesNotContain("OLD", model.PreviewText);
        }
        finally { Directory.Delete(folder, true); }
    }

    [AvaloniaFact]
    public async Task RetryExcludesSkippedPruneRowsAndUsesOnlyFailedRunCount()
    {
        var downloader = new FixtureDownloader();
        var model = Batch(downloader, ["success.mp3", "prune.mp3", "failed.mp3"]);
        model.ParallelCount = 3;
        await model.ScanFolderAsync("fixture");
        await model.StartDownloadCommand.ExecuteAsync(null);
        Assert.Equal(1, model.CompletedCount);
        Assert.Equal(1, model.SkippedCount);
        Assert.Equal(1, model.FailedCount);
        await model.RetryFailedCommand.ExecuteAsync(null);
        Assert.Equal(1, model.TotalCount);
        Assert.Equal(new[] { "failed.mp3" }, downloader.LastFiles);
        Assert.Equal(3, downloader.LastParallelCount);
    }

    [AvaloniaFact]
    public async Task CancellationKeepsUnprocessedRowsOutOfFailuresAndBlocksRestartUntilUnwound()
    {
        var downloader = new FixtureDownloader { WaitForCancellation = true };
        var model = Batch(downloader, ["one.mp3", "two.mp3"]);
        await model.ScanFolderAsync("fixture");
        var running = model.StartDownloadCommand.ExecuteAsync(null);
        await downloader.BatchStarted.Task;
        model.CancelDownloadCommand.Execute(null);
        Assert.True(model.IsDownloading);
        Assert.False(model.StartDownloadCommand.CanExecute(null));
        await running;
        Assert.False(model.IsDownloading);
        Assert.Equal(0, model.FailedCount);
        Assert.All(model.MusicFiles, row => Assert.False(row.IsProcessed));
        Assert.False(model.RetryFailedCommand.CanExecute(null));
    }

    private static LyricsDownloadViewModel Batch(FixtureDownloader downloader, string[]? files = null) =>
        new(downloader, new Dialogs(), new Scanner(files ?? []), new Tags(),
            Options.Create(OptionsValue()));

    private static async Task PumpUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(5);
        }
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class Scanner(string[] files) : IFileScanner
    {
        public Task<List<FileScannerResult>> ScanAsync(string path, IEnumerable<string> extensions) =>
            Task.FromResult(new List<FileScannerResult> { new(".mp3", files.ToList()) });
    }

    private sealed class Tags : ITagLoader
    {
        public ValueTask<MusicInfo?> LoadTagAsync(string path) =>
            ValueTask.FromResult<MusicInfo?>(new(path, Path.GetFileNameWithoutExtension(path), "Artist"));
    }

    private sealed class Dialogs : IDialogService
    {
        public bool WaitForConfirmation { get; init; }
        public TaskCompletionSource ConfirmationStarted { get; } = new();
        public TaskCompletionSource<bool> Confirm { get; } = new();
        public Task<string?> ShowFolderPickerAsync(string title) => Task.FromResult<string?>(null);
        public Task<string?> ShowFilePickerAsync(string title, string[]? filters = null) => Task.FromResult<string?>(null);
        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;
        public Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            ConfirmationStarted.TrySetResult();
            return WaitForConfirmation ? Confirm.Task : Task.FromResult(true);
        }
    }

    private sealed class FixtureProvider : ILyricsProvider
    {
        public bool Wait { get; init; }
        public string DownloaderName => "Fixture";
        public int Calls { get; private set; }
        public TaskCompletionSource Started { get; } = new();
        public TaskCompletionSource Release { get; } = new();
        public async ValueTask<LyricsItemCollection> DownloadAsync(string songName, string artist, long? duration = null)
        {
            Calls++;
            Started.TrySetResult();
            if (Wait) await Release.Task;
            return new LyricsItemCollection(new GlobalLyricsConfigOptions())
                { new LyricsItem("[00:01.00]Fixture lyrics") };
        }
    }

    private sealed class FixtureDownloader(FixtureProvider? provider = null) : ILyricsDownloader
    {
        public FixtureProvider Provider { get; } = provider ?? new();
        public IEnumerable<ILyricsProvider> AvailableProviders => [Provider];
        public bool WaitForCancellation { get; init; }
        public bool WriteLocalLyrics { get; init; }
        public TaskCompletionSource BatchStarted { get; } = new();
        public string[] LastFiles { get; private set; } = [];
        public int LastParallelCount { get; private set; }
        public async Task DownloadAsync(List<MusicInfo> infos, int parallelCount = 1,
            Func<MusicInfo, Task>? callback = null, CancellationToken cancellationToken = default)
        {
            LastFiles = infos.Select(i => i.FilePath).ToArray();
            LastParallelCount = parallelCount;
            BatchStarted.TrySetResult();
            if (WaitForCancellation) await Task.Delay(Timeout.Infinite, cancellationToken);
            for (var i = 0; i < infos.Count; i++)
            {
                infos[i].IsSuccessful = infos[i].FilePath.Contains("success");
                infos[i].IsPruneMusic = infos[i].FilePath.Contains("prune");
                if (WriteLocalLyrics)
                    await File.WriteAllTextAsync(Path.ChangeExtension(infos[i].FilePath, ".lrc"), "[00:01.00]Downloaded lyrics");
                if (callback != null) await callback(infos[i]);
            }
        }
    }
}






