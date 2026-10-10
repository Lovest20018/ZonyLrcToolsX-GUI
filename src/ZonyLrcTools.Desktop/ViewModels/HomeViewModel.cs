using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace ZonyLrcTools.Desktop.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _welcomeMessage = "为你的音乐，补上歌词";

    [ObservableProperty]
    private string _description = "选择文件夹，自动匹配并下载歌词；也可以输入歌名，预览后保存。";

    [ObservableProperty]
    private string _version = $"{typeof(HomeViewModel).Assembly.GetName().Version?.ToString(3)} · Windows GUI";

    [RelayCommand]
    private void Navigate(string target)
    {
        var main = App.Services!.GetRequiredService<MainWindowViewModel>();
        main.SelectedNavigationIndex = int.Parse(target);
    }

    [RelayCommand]
    private void OpenDocumentation() =>
        Process.Start(new ProcessStartInfo("https://docs.myzony.com") { UseShellExecute = true });
}
