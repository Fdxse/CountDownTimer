using System.Globalization;
using System.Text.RegularExpressions;

namespace CountDownTimer;

/// <summary>
/// Turns text typed by the user into a <see cref="TimeSpan"/>.
///
/// Accepted formats:
///   "5"          -> 5 minutes (a plain number means minutes)
///   "1.5"        -> 1 minute 30 seconds
///   "4:30"       -> 4 minutes 30 seconds (mm:ss)
///   "1:15:00"    -> 1 hour 15 minutes (h:mm:ss)
///   "90s", "10m", "2h", "1h 30m", "2m30s" -> units h / m / s
/// </summary>
public static partial class DurationParser
{
    // Matches one "number + unit" chunk such as "10m" or "1.5h".
    [GeneratedRegex(@"(?<amount>\d+(?:[.,]\d+)?)\s*(?<unit>h|m|s)", RegexOptions.IgnoreCase)]
    private static partial Regex UnitChunkRegex();

    // Matches the whole input when it is made up only of "number + unit" chunks.
    [GeneratedRegex(@"^(\s*\d+(?:[.,]\d+)?\s*(h|m|s)\s*)+$", RegexOptions.IgnoreCase)]
    private static partial Regex OnlyUnitChunksRegex();

    /// <summary>
    /// Tries to parse <paramref name="userInput"/>. Returns false for empty,
    /// invalid or zero/negative durations.
    /// </summary>
    public static bool TryParse(string? userInput, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return false;
        }

        string trimmedInput = userInput.Trim();
        bool parsedSuccessfully =
            TryParseColonFormat(trimmedInput, out duration)
            || TryParseUnitFormat(trimmedInput, out duration)
            || TryParsePlainMinutes(trimmedInput, out duration);

        // A countdown of zero (or less) makes no sense.
        return parsedSuccessfully && duration > TimeSpan.Zero;
    }

    /// <summary>Handles "mm:ss" and "h:mm:ss".</summary>
    private static bool TryParseColonFormat(string input, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (!input.Contains(':'))
        {
            return false;
        }

        string[] parts = input.Split(':');
        var numbers = new List<int>();
        foreach (string part in parts)
        {
            if (!int.TryParse(part.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                return false;
            }
            numbers.Add(number);
        }

        switch (numbers.Count)
        {
            case 2: // mm:ss
                duration = TimeSpan.FromMinutes(numbers[0]) + TimeSpan.FromSeconds(numbers[1]);
                return true;
            case 3: // h:mm:ss
                duration = TimeSpan.FromHours(numbers[0])
                           + TimeSpan.FromMinutes(numbers[1])
                           + TimeSpan.FromSeconds(numbers[2]);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Handles "1h 30m", "90s", "2m30s" and similar.</summary>
    private static bool TryParseUnitFormat(string input, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (!OnlyUnitChunksRegex().IsMatch(input))
        {
            return false;
        }

        foreach (Match chunk in UnitChunkRegex().Matches(input))
        {
            double amount = ParseDecimal(chunk.Groups["amount"].Value);
            string unit = chunk.Groups["unit"].Value.ToLowerInvariant();
            duration += unit switch
            {
                "h" => TimeSpan.FromHours(amount),
                "m" => TimeSpan.FromMinutes(amount),
                _ => TimeSpan.FromSeconds(amount),
            };
        }
        return true;
    }

    /// <summary>Handles a plain number, which is read as minutes.</summary>
    private static bool TryParsePlainMinutes(string input, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        string normalizedInput = input.Replace(',', '.');
        if (!double.TryParse(normalizedInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double minutes))
        {
            return false;
        }
        duration = TimeSpan.FromMinutes(minutes);
        return true;
    }

    /// <summary>Parses "1.5" or "1,5" (Swedish decimal comma) the same way.</summary>
    private static double ParseDecimal(string text) =>
        double.Parse(text.Replace(',', '.'), CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a remaining time for display: "mm:ss" below one hour, "h:mm:ss" above.
    /// Seconds are rounded up so the display shows "00:01" until the very end.
    /// </summary>
    public static string FormatForDisplay(TimeSpan remainingTime)
    {
        if (remainingTime < TimeSpan.Zero)
        {
            remainingTime = TimeSpan.Zero;
        }

        long totalWholeSeconds = (long)Math.Ceiling(remainingTime.TotalSeconds);
        long hours = totalWholeSeconds / 3600;
        long minutes = (totalWholeSeconds % 3600) / 60;
        long seconds = totalWholeSeconds % 60;

        return hours > 0
            ? $"{hours}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }
}
