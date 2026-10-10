using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using ZonyLrcTools.Desktop;
using ZonyLrcTools.Desktop.Infrastructure.Localization;
using ZonyLrcTools.Desktop.Services;
using ZonyLrcTools.Desktop.ViewModels;
using Xunit;

namespace ZonyLrcTools.Gui.Tests;

public partial class UiTests
{
    [AvaloniaFact]
    public async Task NewScanRemovesPreviouslyVisibleFailureActions()
    {
        var scanner = new MutableScanner { Files = ["failed.mp3"] };
        var model = new LyricsDownloadViewModel(new FixtureDownloader(), new Dialogs(), scanner, new Tags(),
            Microsoft.Extensions.Options.Options.Create(OptionsValue()));
        await model.ScanFolderAsync("first");
        await model.StartDownloadCommand.ExecuteAsync(null);
        Assert.True(model.HasFailures);
        var visibleFailures = model.HasFailures;
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.HasFailures)) visibleFailures = model.HasFailures;
        };
        scanner.Files = [];
        await model.ScanFolderAsync("empty");
        Assert.False(visibleFailures);
        Assert.False(model.RetryFailedCommand.CanExecute(null));
        scanner.Files = ["success.mp3"];
        await model.ScanFolderAsync("new");
        Assert.False(visibleFailures);
    }

    [AvaloniaFact]
    public void RecreatedHeaderBindingsShareOneObservableResourceSubscription()
    {
        var sources = Enumerable.Range(0, 100).Select(_ =>
            ((Binding)new LocalizeExtension("Column_SongName").ProvideValue(null!)).Source).ToList();
        Assert.Single(sources.Distinct());
        var source = Assert.IsType<LocalizedText>(sources[0]);
        var changed = 0;
        source.CollectionChanged += Changed;
        try
        {
            App.Services!.GetRequiredService<IUILocalizationService>().SetLanguage("en-US");
            Assert.Equal(1, changed);
            Assert.Equal("Song Name", source["Column_SongName"]);
        }
        finally { source.CollectionChanged -= Changed; }
        void Changed(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args) => changed++;
    }

    [AvaloniaTheory]
    [InlineData("en-US", "Error")]
    [InlineData("zh-CN", "错误")]
    public async Task LogExportFailureUsesAFormattedMessageAndTitleWithoutPlaceholder(string culture, string title)
    {
        App.Services!.GetRequiredService<IUILocalizationService>().SetLanguage(culture);
        var folder = Path.Combine(Path.GetTempPath(), "zony-export-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var blocked = Path.Combine(folder, "file");
            await File.WriteAllTextAsync(blocked, "not a folder");
            var dialogs = new ExportDialogs(blocked);
            var model = new LogsViewModel(new AvaloniaWarpLogger(), dialogs);
            await model.ExportCommand.ExecuteAsync(null);
            Assert.Equal(title, dialogs.Title);
            Assert.DoesNotContain("{0}", dialogs.Title);
            Assert.False(string.IsNullOrWhiteSpace(dialogs.Message));
        }
        finally { Directory.Delete(folder, true); }
    }

    private sealed class MutableScanner : ZonyLrcTools.Common.Infrastructure.IO.IFileScanner
    {
        public string[] Files { get; set; } = [];
        public Task<List<ZonyLrcTools.Common.Infrastructure.IO.FileScannerResult>> ScanAsync(string path, IEnumerable<string> extensions) =>
            Task.FromResult(new List<ZonyLrcTools.Common.Infrastructure.IO.FileScannerResult> { new(".mp3", Files.ToList()) });
    }

    private sealed class ExportDialogs(string folder) : IDialogService
    {
        public string Title { get; private set; } = "";
        public string Message { get; private set; } = "";
        public Task<string?> ShowFolderPickerAsync(string title) => Task.FromResult<string?>(folder);
        public Task<string?> ShowFilePickerAsync(string title, string[]? filters = null) => Task.FromResult<string?>(null);
        public Task<bool> ShowConfirmDialogAsync(string title, string message) => Task.FromResult(false);
        public Task ShowMessageAsync(string title, string message) { Title = title; Message = message; return Task.CompletedTask; }
    }
}
