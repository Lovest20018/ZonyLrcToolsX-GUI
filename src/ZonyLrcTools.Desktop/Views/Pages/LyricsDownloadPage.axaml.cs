using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.ComponentModel;
using ZonyLrcTools.Desktop.ViewModels;

namespace ZonyLrcTools.Desktop.Views.Pages;

public partial class LyricsDownloadPage : UserControl
{
    private LyricsDownloadViewModel? _model;
    public LyricsDownloadPage()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdatePreviewLayout();
        DataContextChanged += (_, _) => ObserveModel();
        AttachedToVisualTree += (_, _) => ObserveModel();
        DetachedFromVisualTree += (_, _) =>
        {
            if (_model != null) _model.PropertyChanged -= OnModelChanged;
            _model = null;
        };
    }
    private void ObserveModel()
    {
        if (_model != null) _model.PropertyChanged -= OnModelChanged;
        _model = DataContext as LyricsDownloadViewModel;
        if (_model != null)
        {
            _model.PropertyChanged += OnModelChanged;
            SongsGrid.Columns[3].IsVisible = _model.ShowPathColumn;
        }
        UpdatePreviewLayout();
    }
    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LyricsDownloadViewModel.IsPreviewOpen)) UpdatePreviewLayout();
        if (e.PropertyName == nameof(LyricsDownloadViewModel.ShowPathColumn))
            SongsGrid.Columns[3].IsVisible = _model?.ShowPathColumn == true;
    }
    private void UpdatePreviewLayout()
    {
        var wide = Bounds.Width >= 840;
        var open = DataContext is LyricsDownloadViewModel { IsPreviewOpen: true, IsLyrics: true };
        Workspace.ColumnDefinitions[1].Width = new GridLength(wide && open ? 274 : 0);
        // The narrow drawer stays inside the list so Stop and the task footer remain reachable.
        Grid.SetRow(PreviewPanel, 4);
        Grid.SetRowSpan(PreviewPanel, 1);
        PreviewPanel.Width = wide ? 260 : Math.Min(280, Bounds.Width);
        PreviewPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
        PreviewPanel.Margin = new Thickness(0);
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is LyricsDownloadViewModel model && model.IsPreviewOpen)
        {
            model.ClosePreviewCommand.Execute(null);
            e.Handled = true;
        }
    }
}
