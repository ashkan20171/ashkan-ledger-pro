using System;
using System.IO;
using System.Text.Json;

namespace AshkanLedgerPro;

/// <summary>Persists visual preferences separately from financial data.</summary>
public static class ThemePreferences
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AshkanLedgerPro", "ui-theme.json");

    public static bool IsLight()
    {
        try
        {
            if (!File.Exists(FilePath)) return false;
            return JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(FilePath))?.Light ?? false;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (JsonException) { return false; }
    }

    public static void Save(bool light)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new ThemeSettings(light)));
        File.Move(temp, FilePath, true);
    }

    private sealed record ThemeSettings(bool Light);
}
