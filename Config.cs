using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace PiHoleTray;

class AppConfig
{
    [JsonPropertyName("pihole_url")]
    public string PiholeUrl { get; set; } = "http://pi.hole";

    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("api_version")]
    public int ApiVersion { get; set; } = 6;

    [JsonPropertyName("autostart")]
    public bool Autostart { get; set; } = false;

    [JsonPropertyName("poll_interval")]
    public int PollInterval { get; set; } = 10;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "";
}

static class ConfigManager
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiHoleTray");

    public static readonly string ConfigPath = Path.Combine(AppDataDir, "config.json");
    public static readonly string LogPath    = Path.Combine(AppDataDir, "pihole_tray.log");

    static ConfigManager()
    {
        Directory.CreateDirectory(AppDataDir);
    }

    public static AppConfig Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch { }
        }
        return new AppConfig();
    }

    public static void Save(AppConfig cfg)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(cfg, options));
    }

    public static void SetAutostart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key == null) return;
            if (enable)
            {
                var exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                key.SetValue("PiHoleTray", $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue("PiHoleTray", throwOnMissingValue: false);
            }
        }
        catch { }
    }
}
