using System.IO;
using System.Text.Json;
using System.Windows;

namespace CountDownTimer;

/// <summary>
/// User settings read from "settings.json" next to CountDownTimer.exe.
/// Uses System.Text.Json, which is part of .NET (no extra package).
/// If the file is missing it is created with the default values,
/// so there is always a file to edit.
/// </summary>
public sealed class TimerSettings
{
    /// <summary>Name of the config file, placed in the same folder as the exe.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>Text shown (flashing) when the countdown reaches zero.</summary>
    public string FinishedText { get; set; } = "TimeOut!";

    /// <summary>The digits start flashing when this many seconds (or fewer) are left. 0 = only flash at the end.</summary>
    public int FlashWhenSecondsLeft { get; set; } = 10;

    /// <summary>Full path of the settings file.</summary>
    public static string SettingsFilePath => Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    // Settings shared by loading and saving: readable, indented JSON and
    // property names that don't care about upper/lower case.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Reads the settings file. Falls back to defaults (and tells the user)
    /// if the file can't be read, so a typo never stops the timer from working.
    /// </summary>
    public static TimerSettings Load()
    {
        string settingsPath = SettingsFilePath;

        if (!File.Exists(settingsPath))
        {
            var defaultSettings = new TimerSettings();
            TryWriteDefaults(defaultSettings, settingsPath);
            return defaultSettings;
        }

        try
        {
            string fileContent = File.ReadAllText(settingsPath);
            TimerSettings loadedSettings = JsonSerializer.Deserialize<TimerSettings>(fileContent, JsonOptions)
                                           ?? new TimerSettings();
            loadedSettings.CleanUpValues();
            return loadedSettings;
        }
        catch (Exception readError) when (readError is JsonException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Could not read {settingsPath}:\n{readError.Message}\n\nThe default settings are used instead.",
                "Countdown timer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return new TimerSettings();
        }
    }

    /// <summary>Replaces missing or nonsense values with something sensible.</summary>
    private void CleanUpValues()
    {
        // Allow an intentionally empty text, but not a missing one ("null" in the file).
        FinishedText ??= "TimeOut!";

        if (FlashWhenSecondsLeft < 0)
        {
            FlashWhenSecondsLeft = 0;
        }
    }

    /// <summary>
    /// Creates the settings file with default values. Failing to write it
    /// (for example in a read-only folder) is not a problem: the defaults are still used.
    /// </summary>
    private static void TryWriteDefaults(TimerSettings defaultSettings, string settingsPath)
    {
        try
        {
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(defaultSettings, JsonOptions));
        }
        catch (Exception writeError) when (writeError is IOException or UnauthorizedAccessException)
        {
            // Ignored on purpose: the app works fine without the file.
        }
    }
}
