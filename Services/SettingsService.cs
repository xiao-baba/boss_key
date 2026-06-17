using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BossKey.Models;

namespace BossKey.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BossKey");

    private string SettingsPath => Path.Combine(ConfigDirectory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            settings.HideHotkey ??= HotkeyGesture.DefaultHide();
            settings.RestoreHotkey ??= HotkeyGesture.DefaultRestore();
            settings.HideRules ??= [];
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
