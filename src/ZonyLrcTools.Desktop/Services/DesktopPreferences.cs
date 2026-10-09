using System.Text.Json;

namespace ZonyLrcTools.Desktop.Services;

public sealed class DesktopPreferences
{
    public bool FollowSystemLanguage { get; set; }
    public string Culture { get; set; } = "zh-CN";

    public static string DataDirectory => Environment.GetEnvironmentVariable("ZONY_LRC_GUI_DATA_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZonyLrcToolsX.Gui");
    public static string ConfigPath => Path.Combine(DataDirectory, "config.yaml");
    public static string PreferencesPath => Path.Combine(DataDirectory, "desktop.json");

    public static DesktopPreferences Load()
    {
        Directory.CreateDirectory(DataDirectory);
        try
        {
            return File.Exists(PreferencesPath)
                ? JsonSerializer.Deserialize<DesktopPreferences>(File.ReadAllText(PreferencesPath)) ?? new()
                : new();
        }
        catch (JsonException) { return new(); }
    }

    public void Save()
    {
        var temporary = PreferencesPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, PreferencesPath, true);
    }
}
