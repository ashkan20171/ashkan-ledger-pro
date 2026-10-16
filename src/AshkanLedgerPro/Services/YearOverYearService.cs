using AshkanLedgerPro.Models;
using System.Globalization;
using System.Text;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only comparison of two Gregorian calendar years, using posted journal entries.</summary>
public static class YearOverYearService
{
 public sealed record Comparison(int Month, decimal PreviousRevenue, decimal CurrentRevenue,
  decimal PreviousExpenses, decimal CurrentExpenses)
 {
  public string MonthLabel => Month.ToString("00", CultureInfo.InvariantCulture);
  public decimal PreviousProfit => PreviousRevenue - PreviousExpenses;
  public decimal CurrentProfit => CurrentRevenue - CurrentExpenses;
  public decimal ProfitChange => CurrentProfit - PreviousProfit;
  public decimal? ProfitChangePercent => PreviousProfit == 0 ? null : ProfitChange / Math.Abs(PreviousProfit) * 100m;
 }

 public static IReadOnlyList<Comparison> Compare(IEnumerable<Account> accounts,
  IEnumerable<JournalEntry> journal, int currentYear)
 {
  if (currentYear < 1901 || currentYear > 9998) throw new ArgumentOutOfRangeException(nameof(currentYear));
  var entries = journal.ToList();
  var prior = ManagementAnalyticsService.ForYear(accounts, entries, currentYear - 1);
  var current = ManagementAnalyticsService.ForYear(accounts, entries, currentYear);
  return Enumerable.Range(0, 12).Select(i => new Comparison(i + 1,
   prior[i].Revenue, current[i].Revenue, prior[i].Expenses, current[i].Expenses)).ToList();
 }

 public static string ToCsv(IEnumerable<Comparison> rows)
 {
  var sb = new StringBuilder("Month,PreviousRevenue,CurrentRevenue,PreviousExpenses,CurrentExpenses,PreviousProfit,CurrentProfit,ProfitChange,ProfitChangePercent\r\n");
  foreach (var r in rows)
  {
   var values = new [] { r.PreviousRevenue, r.CurrentRevenue, r.PreviousExpenses, r.CurrentExpenses,
    r.PreviousProfit, r.CurrentProfit, r.ProfitChange };
   sb.Append(r.MonthLabel);
   foreach (var value in values) sb.Append(',').Append(value.ToString(CultureInfo.InvariantCulture));
   sb.Append(',').Append(r.ProfitChangePercent?.ToString("0.##", CultureInfo.InvariantCulture) ?? "N/A").Append("\r\n");
  }
  return sb.ToString();
 }
}
