using System.ComponentModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace ZonyLrcTools.Desktop.Infrastructure.Localization;

/// <summary>Observable resource lookup, so already-open views follow language changes.</summary>
public sealed class LocalizedText : INotifyPropertyChanged, INotifyCollectionChanged
{
    private static readonly ResourceManager Resources = new("ZonyLrcTools.Desktop.UIStrings", typeof(App).Assembly);
    private static readonly ConditionalWeakTable<IUILocalizationService, LocalizedText> SharedSources = new();
    public static LocalizedText Shared
    {
        get
        {
            var localization = App.Services?.GetService<IUILocalizationService>();
            return localization == null ? new LocalizedText(null) :
                SharedSources.GetValue(localization, service => new LocalizedText(service));
        }
    }
    private readonly IUILocalizationService? _localization;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public LocalizedText() : this(App.Services?.GetService<IUILocalizationService>()) { }
    private LocalizedText(IUILocalizationService? localization)
    {
        _localization = localization;
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
