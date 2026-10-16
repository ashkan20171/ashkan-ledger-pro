using AshkanLedgerPro.Models;
using System.Globalization;
using System.Text;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only expense budget comparison for posted journal entries.</summary>
public static class BudgetAnalyticsService
{
 public sealed record BudgetResult(int AccountId, string Code, string Name, decimal Limit, decimal Actual)
 {
  public decimal Remaining => Limit - Actual;
  public decimal? UsedPercent => Limit == 0m ? null : Math.Round(Actual / Limit * 100m, 2);
  public bool Exceeded => Actual > Limit;
 }

 public static BudgetResult Evaluate(Account account, IEnumerable<JournalEntry> journal,
  DateTime from, DateTime to, decimal limit)
 {
  ArgumentNullException.ThrowIfNull(account);
  if (account.Type != "Expense") throw new ArgumentException("An expense account is required.", nameof(account));
  if (from.Date > to.Date) throw new ArgumentException("Start date exceeds end date.");
  if (limit < 0m) throw new ArgumentOutOfRangeException(nameof(limit));
  decimal actual = 0m;
  foreach (var entry in journal)
  {
   if (!entry.Posted || entry.Date.Date < from.Date || entry.Date.Date > to.Date) continue;
   foreach (var line in entry.Lines)
    if (line.AccountId == account.Id) actual += line.Debit - line.Credit;
  }
  return new BudgetResult(account.Id, account.Code, account.Name, limit, actual);
 }

 public static string ToCsv(BudgetResult result, DateTime from, DateTime to)
 {
  static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
  static string N(decimal n) => n.ToString(CultureInfo.InvariantCulture);
  return "AccountCode,AccountName,From,To,Budget,Actual,Remaining,UsedPercent,Exceeded\r\n"
   + Quote(result.Code) + "," + Quote(result.Name) + ","
   + from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ","
   + to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ","
   + N(result.Limit) + "," + N(result.Actual) + "," + N(result.Remaining) + ","
   + (result.UsedPercent is decimal used ? N(used) : "N/A") + ","
   + (result.Exceeded ? "true" : "false") + "\r\n";
 }
}
