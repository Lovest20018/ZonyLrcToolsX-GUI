using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZonyLrcTools.Desktop;
using ZonyLrcTools.Desktop.ViewModels;

namespace ZonyLrcTools.Gui.Tests;

public class LiveDownloadTests
{
    [AvaloniaFact]
    [Trait("Category", "Live")]
    public async Task MatchesAndSavesRealLyricsThroughDesktopCommands()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "LiveDownload", Guid.NewGuid().ToString("N"));
        var manual = App.Services!.GetRequiredService<ManualDownloadViewModel>();
        manual.SongName = "七里香";
        manual.Artist = "周杰伦";
        manual.OutputFolder = folder;
        await manual.MatchCommand.ExecuteAsync(null);
        Assert.True(manual.HasLyrics, manual.Status);
        Assert.Contains("[", manual.Preview);
        await manual.SaveCommand.ExecuteAsync(null);
        var path = Path.Combine(folder, "周杰伦 - 七里香.lrc");
        Assert.True(File.Exists(path), manual.Status);
        Assert.True(new FileInfo(path).Length > 100);
    }
}
