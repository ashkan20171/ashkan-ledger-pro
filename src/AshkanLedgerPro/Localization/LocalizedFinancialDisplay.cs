using System;
using System.Globalization;

namespace AshkanLedgerPro.Localization;

/// <summary>Presentation-only formatting: changing language never converts currency.</summary>
public static class LocalizedFinancialDisplay
{
    public static string FormatAmount(decimal amount, bool persian, string currencyCode)
    {
        var culture = CultureInfo.GetCultureInfo(persian ? "fa-IR" : "en-US");
        var currency = currencyCode.ToUpperInvariant() switch
        {
            "IRR" => persian ? "ریال" : "IRR",
            "IRT" => persian ? "تومان" : "toman",
            "USD" => persian ? "دلار" : "USD",
            "EUR" => persian ? "یورو" : "EUR",
            _ => currencyCode
        };
        return $"{amount.ToString("N2", culture)} {currency}";
    }

    public static string FormatDate(DateTime date, bool persian)
    {
        if (!persian) return date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00}";
    }
}
