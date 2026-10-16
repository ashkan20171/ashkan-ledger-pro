using System;
using System.Globalization;
using System.Linq;

namespace AshkanLedgerPro.Localization;

/// <summary>Validates date input using the active calendar, without changing stored Gregorian DateTime values.</summary>
public static class LocalizedDateInput
{
    public static string Format(DateTime value, bool persian) =>
        LocalizedFinancialDisplay.FormatDate(value, persian);

    public static bool TryParse(string? text, bool persian, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = NormalizeDigits(text.Trim()).Replace('-', '/');
        var parts = normalized.Split('/');
        if (parts.Length != 3 || parts.Any(p => p.Length == 0) ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int year) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int month) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int day)) return false;
        try
        {
            date = persian ? new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0)
                           : new DateTime(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    public static string NormalizeDigits(string input)
    {
        const string persian = "۰۱۲۳۴۵۶۷۸۹";
        const string arabic = "٠١٢٣٤٥٦٧٨٩";
        return new string(input.Select(c =>
        {
            int p = persian.IndexOf(c);
            if (p >= 0) return (char)('0' + p);
            int a = arabic.IndexOf(c);
            return a >= 0 ? (char)('0' + a) : c;
        }).ToArray());
    }
}
