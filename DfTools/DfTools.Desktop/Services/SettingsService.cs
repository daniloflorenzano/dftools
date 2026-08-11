using System;
using System.IO;
using System.Text.Json;
using DfTools.Desktop.Generated;
using DfTools.Desktop.Models;

namespace DfTools.Desktop.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SettingsFilePath { get; }

    public SettingsService()
    {
        // Settings file resides alongside app binaries / source directory for developers to edit directly
        SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Fallback to default generated settings if file read error occurs
        }

        return GeneratedAppSettings.Default;
    }

    public bool SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
