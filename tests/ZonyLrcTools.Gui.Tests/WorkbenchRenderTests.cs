using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ZonyLrcTools.Desktop;
using ZonyLrcTools.Desktop.Infrastructure.Localization;
using ZonyLrcTools.Desktop.ViewModels;
using Xunit;

namespace ZonyLrcTools.Gui.Tests;

public partial class UiTests
{
    [AvaloniaFact]
    public async Task RenderWorkbenchMatrixAndPopulatedStates()
    {
        var folder = Environment.GetEnvironmentVariable("ZONY_GUI_TEST_ARTIFACTS");
        if (folder == null) return;
        Directory.CreateDirectory(folder);
        var localization = App.Services!.GetRequiredService<IUILocalizationService>();
        var main = App.Services!.GetRequiredService<MainWindowViewModel>();
        var original = localization.CurrentCulture;
        var originalTheme = Application.Current!.RequestedThemeVariant;
        var batch = App.Services!.GetRequiredService<LyricsDownloadViewModel>();
        var window = CreateWindow();
        window.Show();
        try
        {
            foreach (var language in new[] { "zh-CN", "en-US" })
            foreach (var dark in new[] { false, true })
            foreach (var size in new[] { new PixelSize(900, 600), new PixelSize(1200, 800), new PixelSize(1440, 900) })
            {
                localization.SetLanguage(language);
                Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                window.Close();
                window = new ZonyLrcTools.Desktop.Views.MainWindow { DataContext = main, Width = size.Width, Height = size.Height };
                window.Show();
                for (var index = 0; index < 6; index++)
                {
                    main.SelectedNavigationIndex = index;
                    await Capture($"{language}-{(dark ? "dark" : "light")}-{size.Width}x{size.Height}-page{index}", size);
                }
            }
            var fixture = Path.Combine(folder, "render-fixture");
            Directory.CreateDirectory(fixture);
            var musicPath = Path.Combine(fixture, "很长的中文路径与歌名 - Selected song.mp3");
            await File.WriteAllTextAsync(Path.ChangeExtension(musicPath, ".lrc"),
                "[00:01.00]让歌词随音乐同行\n[00:05.20]A local preview, with no network request\n[00:09.00]第二行中文歌词");

            batch.MusicFiles.Clear();
            batch.SelectedFolderPath = @"D:\音乐资料\无损音乐\2026 精选音乐合集\很长的中文路径用于检查工具栏可达性";
            for (var i = 0; i < 5000; i++)
            {
                var row = new MusicFileViewModel { Name = $"歌曲 {i + 1} · A song with a longer title", Artist = "歌手 Artist",
                    FilePath = i == 0 ? musicPath : $"D:/Music/song-{i}.mp3",
                    IsProcessed = i < 32, IsSuccessful = i < 27, IsSkipped = i is >=27 and <30 };
                row.RefreshStatus(batch.Text);
                batch.MusicFiles.Add(row);
            }
            batch.TotalCount = 5000;
            batch.CompletedCount = 27;
            batch.SkippedCount = 3;
            batch.FailedCount = 2;
            batch.SelectedMusicFile = batch.MusicFiles[0];
            await PumpUntil(() => batch.PreviewText.Contains("让歌词"));
            var manual = new ManualDownloadViewModel(new FixtureDownloader(), new Dialogs(),
                new ZonyLrcTools.Desktop.Services.AvaloniaWarpLogger(), Microsoft.Extensions.Options.Options.Create(OptionsValue()))
                { SongName = "七里香 · Submitted song", Artist = "周杰伦 · Artist" };
            await manual.MatchCommand.ExecuteAsync(null);
            foreach (var language in new[] { "zh-CN", "en-US" })
            foreach (var dark in new[] { false, true })
            foreach (var size in new[] { new PixelSize(900, 600), new PixelSize(1200, 800), new PixelSize(1440, 900) })
            {
                localization.SetLanguage(language);
                Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
                window.Close();
                window = new ZonyLrcTools.Desktop.Views.MainWindow { DataContext = main, Width = size.Width, Height = size.Height };
                window.Show();
                main.SelectedNavigationIndex = 1;
                main.CurrentPage = batch;
                batch.IsPreviewOpen = true;
                await Capture($"{language}-{(dark ? "dark" : "light")}-{size.Width}x{size.Height}-populated-batch", size);
                batch.IsDownloading = true;
                await Capture($"{language}-{(dark ? "dark" : "light")}-{size.Width}x{size.Height}-running-batch", size);
                batch.IsDownloading = false;
                main.SelectedNavigationIndex = 2;
                main.CurrentPage = manual;
                await Capture($"{language}-{(dark ? "dark" : "light")}-{size.Width}x{size.Height}-matched-single", size);
                manual.ShowRawLrc = true;
                await Capture($"{language}-{(dark ? "dark" : "light")}-{size.Width}x{size.Height}-raw-single", size);
                manual.ShowRawLrc = false;
            }
            batch.MusicFiles.Clear();
            batch.SelectedMusicFile = null;
            main.SelectedNavigationIndex = 0;

            async Task Capture(string name, PixelSize size)
            {
                Dispatcher.UIThread.RunJobs();
                window.Measure(new Size(size.Width, size.Height));
                window.Arrange(new Rect(0, 0, size.Width, size.Height));
                window.UpdateLayout();
                Assert.Equal(size.Width, window.Bounds.Width);
                Assert.Equal(size.Height, window.Bounds.Height);
                Dispatcher.UIThread.RunJobs();
                if (name.Contains("batch"))
                {
                    Assert.Same(batch, main.CurrentPage);
                    batch.SelectedMusicFile = batch.MusicFiles[0];
                    await PumpUntil(() => batch.PreviewText.Contains("让歌词"));
                    Assert.Contains("让歌词", batch.PreviewText);
                    window.UpdateLayout();
                }
                using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
                bitmap.Render(window);
                bitmap.Save(Path.Combine(folder, name + ".png"));
            }
        }
        finally
        {
            localization.SetLanguage(original);
            Application.Current.RequestedThemeVariant = originalTheme;
            window.Close();
        }
    }
}
