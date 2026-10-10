using System.ComponentModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace ZonyLrcTools.Desktop.Infrastructure.Localization;

/// <summary>Observable resource lookup, so already-open views follow language changes.</summary>
public sealed class LocalizedText : INotifyPropertyChanged, INotifyCollectionChanged
{
    private static readonly ResourceManager Resources = new("ZonyLrcTools.Desktop.UIStrings", typeof(App).Assembly);
    private readonly IUILocalizationService? _localization;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public LocalizedText()
    {
        _localization = App.Services?.GetService<IUILocalizationService>();
        if (_localization != null)
            _localization.LanguageChanged += (_, _) =>
            {
                PropertyChanged?.Invoke(this, new("Item[]"));
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
            };
    }
    private CultureInfo Culture => _localization == null ? CultureInfo.CurrentUICulture : CultureInfo.GetCultureInfo(_localization.CurrentCulture);
    public string this[string key] => Resources.GetString(key, Culture) ?? $"[{key}]";
    public string Format(string key, params object[] arguments) =>
        string.Format(Culture, this[key], arguments);
}

