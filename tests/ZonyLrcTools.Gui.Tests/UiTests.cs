using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZonyLrcTools.Desktop;
using ZonyLrcTools.Desktop.Services;
using ZonyLrcTools.Desktop.ViewModels;
using ZonyLrcTools.Desktop.Views;

[assembly: AvaloniaTestApplication(typeof(ZonyLrcTools.Gui.Tests.UiTestApp))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ZonyLrcTools.Gui.Tests;

public static class UiTestApp
{
    public static AppBuilder BuildAvaloniaApp()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var data = Path.Combine(AppContext.BaseDirectory, "GuiTestData");
        Directory.CreateDirectory(data);
        Environment.SetEnvironmentVariable("ZONY_LRC_GUI_DATA_DIR", data);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "config.yaml"), Path.Combine(data, "config.yaml"), true);
        return AppBuilder.Configure<App>().UseSkia().WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}

public class UiTests
{
    private static MainWindow CreateWindow() => new()
    {
        DataContext = App.Services!.GetRequiredService<MainWindowViewModel>(),
        Width = 1200,
        Height = 800
    };

    [AvaloniaFact]
    public void EveryNavigationPageLoadsAndRenders()
    {
        var main = App.Services!.GetRequiredService<MainWindowViewModel>();
        var window = CreateWindow();
        window.Show();
        try
        {
            var types = new[] { typeof(HomeViewModel), typeof(LyricsDownloadViewModel),
                typeof(AlbumDownloadViewModel), typeof(SettingsViewModel), typeof(ManualDownloadViewModel),
                typeof(LogsViewModel) };
            for (var index = 0; index < types.Length; index++)
            {
                main.SelectedNavigationIndex = index;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Assert.Equal(types[index], main.CurrentPage!.GetType());
                Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),
                    t => !string.IsNullOrEmpty(t.Text));
                var folder = Environment.GetEnvironmentVariable("ZONY_GUI_TEST_ARTIFACTS");
                if (folder != null)
                {
                    Directory.CreateDirectory(folder);
                    using var bitmap = new RenderTargetBitmap(new PixelSize(1200, 800), new Vector(96, 96));
                    bitmap.Render(window);
                    bitmap.Save(Path.Combine(folder, $"{index}-{types[index].Name}.png"));
                }
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task ScanningRealMusicEnablesStartCommandAndShowsTags()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "ScanFixture");
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Sample.mp3"), Path.Combine(folder, "test.mp3"), true);
        var viewModel = App.Services!.GetRequiredService<LyricsDownloadViewModel>();
        await viewModel.ScanFolderAsync(folder);
        Assert.Single(viewModel.MusicFiles);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.MusicFiles[0].Name));
        Assert.True(viewModel.StartDownloadCommand.CanExecute(null));
        Assert.False(viewModel.IsScanning);
    }

    [AvaloniaFact]
    public void ProviderChangesPersistToYamlAndTakeEffectImmediately()
    {
        var settings = App.Services!.GetRequiredService<SettingsViewModel>();
        var provider = settings.LyricsProviders.First(p => p.Name == "NetEase");
        provider.IsEnabled = false;
        var text = File.ReadAllText(DesktopPreferences.ConfigPath);
        Assert.Contains("priority: -1", text);
        var downloader = App.Services!.GetRequiredService<ZonyLrcTools.Common.Lyrics.ILyricsDownloader>();
        Assert.DoesNotContain(downloader.AvailableProviders, p => p.DownloaderName == "NetEase");
        provider.IsEnabled = true;
        provider.Priority = 5;
        Assert.Equal("NetEase", downloader.AvailableProviders.Last().DownloaderName);
        Assert.Contains("priority: 5", File.ReadAllText(DesktopPreferences.ConfigPath));
        provider.Priority = 1;
    }
}
