using System;
using System.IO;
using System.Text.Json;

namespace AshkanLedgerPro;

public static class LanguagePreferences
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AshkanLedgerPro", "ui-language.json");

    public static bool IsPersian()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return true;
            var data = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath));
            return data?.Language != "en";
        }
        catch (IOException) { return true; }
        catch (JsonException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    public static void Save(bool persian)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new Settings(persian ? "fa" : "en")));
        File.Move(temp, SettingsPath, true);
    }

    private sealed record Settings(string Language);
}
