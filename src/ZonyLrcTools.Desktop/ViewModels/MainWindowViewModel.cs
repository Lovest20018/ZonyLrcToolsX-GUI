using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZonyLrcTools.Desktop.Infrastructure.Localization;
using ZonyLrcTools.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ZonyLrcTools.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IUILocalizationService _localization;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeButtonText))]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _title = "ZonyLrcTools X";

    // Navigation localized strings
    public string NavHome => _localization["Nav_Home"];
    public string NavLyricsDownload => _localization["Nav_LyricsDownload"];
    public string NavAlbumDownload => _localization["Nav_AlbumDownload"];
    public string NavSettings => _localization["Nav_Settings"];
    public string HomeDescription => _localization["Home_Description"];
    public bool IsHome => SelectedNavigationIndex == 0;
    public bool IsBatch => SelectedNavigationIndex == 1;
    public bool IsManual => SelectedNavigationIndex == 2;
    public bool IsAlbum => SelectedNavigationIndex == 3;
    public bool IsLogs => SelectedNavigationIndex == 4;
    public bool IsSettings => SelectedNavigationIndex == 5;
    public bool IsCompact => false;

    partial void OnSelectedNavigationIndexChanged(int value)
    {
        var services = App.Services;
        if (services != null)
            CurrentPage = value switch
            {
                0 => services.GetRequiredService<HomeViewModel>(),
                1 => services.GetRequiredService<LyricsDownloadViewModel>(),
                2 => services.GetRequiredService<ManualDownloadViewModel>(),
                3 => services.GetRequiredService<AlbumDownloadViewModel>(),
                4 => services.GetRequiredService<LogsViewModel>(),
                5 => services.GetRequiredService<SettingsViewModel>(),
                _ => CurrentPage
            };
        foreach (var name in new[] { nameof(IsHome), nameof(IsBatch), nameof(IsManual), nameof(IsAlbum), nameof(IsLogs), nameof(IsSettings) })
            OnPropertyChanged(name);
    }
    [RelayCommand] private void Navigate(string index) => SelectedNavigationIndex = int.Parse(index);
    [RelayCommand] private void Help() => System.Diagnostics.Process.Start(
        new System.Diagnostics.ProcessStartInfo("https://docs.myzony.com") { UseShellExecute = true });

    public string ThemeButtonText => IsDarkTheme
        ? _localization["Settings_ThemeLight"]
        : _localization["Settings_ThemeDark"];

    public MainWindowViewModel(
        IThemeService themeService,
        IUILocalizationService localization,
        HomeViewModel homeViewModel)
    {
        _themeService = themeService;
        _localization = localization;
        IsDarkTheme = themeService.CurrentTheme == Avalonia.Styling.ThemeVariant.Dark;
        themeService.ThemeChanged += (_, theme) => IsDarkTheme = theme == Avalonia.Styling.ThemeVariant.Dark;
        if (Avalonia.Application.Current is { } app)
            app.ActualThemeVariantChanged += (_, _) => IsDarkTheme = app.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;

        // Set initial page
        CurrentPage = homeViewModel;
        SelectedNavigationIndex = 0;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        // Notify all localized properties have changed
        OnPropertyChanged(nameof(NavHome));
        OnPropertyChanged(nameof(NavLyricsDownload));
        OnPropertyChanged(nameof(NavAlbumDownload));
        OnPropertyChanged(nameof(NavSettings));
        OnPropertyChanged(nameof(HomeDescription));
        OnPropertyChanged(nameof(ThemeButtonText));
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _themeService.SetTheme(IsDarkTheme
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light);
    }
}
