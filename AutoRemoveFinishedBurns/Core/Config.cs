using System;
using System.Globalization;
using System.IO;
using Brutal.Logging;

namespace AutoRemoveFinishedBurns.Core;

/// <summary>
/// Persists the in-game toggle to a TOML file in the mod's user directory.
/// Defaults to enabled.
/// </summary>
static class Config
{
    private static string _modDir = string.Empty;
    private static string _configPath = string.Empty;

    public static bool Enabled { get; set; } = true;

    public static void Init()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _modDir = Path.Combine(userProfile, "My Games", "Kitten Space Agency",
            "mods", "AutoRemoveFinishedBurns");
        _configPath = Path.Combine(_modDir, "autoremovefinishedburns.toml");

        Load();
    }

    public static void Reset()
    {
        Enabled = true;
    }

    public static void Load()
    {
        if (!File.Exists(_configPath))
        {
            Save();
            return;
        }

        try
        {
            foreach (string rawLine in File.ReadAllLines(_configPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                int eq = line.IndexOf('=');
                if (eq < 1) continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                int commentIdx = value.IndexOf('#');
                if (commentIdx >= 0)
                    value = value.Substring(0, commentIdx).Trim();

                if (key == "enabled" && bool.TryParse(value, out bool b))
                    Enabled = b;
            }

            if (DebugConfig.Settings)
                DefaultCategory.Log.Debug(
                    $"[AutoRemoveFinishedBurns] Config loaded: enabled={Enabled}");
        }
        catch (Exception ex)
        {
            DefaultCategory.Log.Error(
                $"[AutoRemoveFinishedBurns] Failed to load config: {ex.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(_modDir);
            using var writer = new StreamWriter(_configPath);
            writer.WriteLine("# AutoRemoveFinishedBurns configuration.");
            writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "enabled = {0}", Enabled ? "true" : "false"));

            if (DebugConfig.Settings)
                DefaultCategory.Log.Debug(
                    $"[AutoRemoveFinishedBurns] Config saved: enabled={Enabled}");
        }
        catch (Exception ex)
        {
            DefaultCategory.Log.Error(
                $"[AutoRemoveFinishedBurns] Failed to save config: {ex.Message}");
        }
    }
}
