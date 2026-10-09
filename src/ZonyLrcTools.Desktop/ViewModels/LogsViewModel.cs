using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using ZonyLrcTools.Desktop.Services;

namespace ZonyLrcTools.Desktop.ViewModels;

public partial class LogsViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;
    public ObservableCollection<LogEntry> Entries { get; }

    public LogsViewModel(AvaloniaWarpLogger logger, IDialogService dialogs)
    {
        Entries = logger.LogEntries;
        _dialogs = dialogs;
    }

    [RelayCommand] private void Clear() => Entries.Clear();

    [RelayCommand]
    private async Task ExportAsync()
    {
        var folder = await _dialogs.ShowFolderPickerAsync("选择日志保存目录");
        if (folder == null) return;
        try
        {
            var path = Path.Combine(folder, $"ZonyLrcTools_日志_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var lines = Entries.Select(e => $"{e.Timestamp:yyyy-MM-dd HH:mm:ss} [{e.Level}] {e.Message} {e.Exception}").ToArray();
            await File.WriteAllLinesAsync(path, lines, new UTF8Encoding(true));
            await _dialogs.ShowMessageAsync("日志已导出", path);
        }
        catch (Exception ex) { await _dialogs.ShowMessageAsync("导出失败", ex.Message); }
    }
}
