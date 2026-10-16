using AshkanLedgerPro.Models;
using System.Globalization;
using System.Text;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only management reporting. Uses posted journal lines; does not convert currencies.</summary>
public static class ManagementAnalyticsService
{
 public sealed record MonthSummary(int Year, int Month, decimal Revenue, decimal Expenses)
 {
  public decimal NetProfit => Revenue - Expenses;
  public string Period => $"{Year:D4}-{Month:D2}";
 }

 public static IReadOnlyList<MonthSummary> ForYear(IEnumerable<Account> accounts,
  IEnumerable<JournalEntry> journal, int year)
 {
  if (year < 1900 || year > 9998) throw new ArgumentOutOfRangeException(nameof(year));
  var types = accounts.ToDictionary(a => a.Id, a => a.Type);
  var totals = new (decimal Revenue, decimal Expenses)[12];
  foreach (var entry in journal)
  {
   if (!entry.Posted || entry.Date.Year != year) continue;
   var index = entry.Date.Month - 1;
   foreach (var line in entry.Lines)
   {
    if (!types.TryGetValue(line.AccountId, out var type)) continue;
    if (type == "Revenue") totals[index].Revenue += line.Credit - line.Debit;
    else if (type == "Expense") totals[index].Expenses += line.Debit - line.Credit;
   }
  }
  return Enumerable.Range(1, 12)
   .Select(month => new MonthSummary(year, month, totals[month - 1].Revenue, totals[month - 1].Expenses))
   .ToList();
 }

 public sealed record PeriodTotals(DateTime Start, DateTime End, decimal Revenue, decimal Expenses)
 {
  public decimal NetProfit => Revenue - Expenses;
  public decimal? MarginPercent => Revenue == 0m ? null : Math.Round(NetProfit / Revenue * 100m, 2);
 }

 /// <summary>Inclusive date boundaries; posted entries only, in the ledger's original currency.</summary>
 public static PeriodTotals ForPeriod(IEnumerable<Account> accounts,
  IEnumerable<JournalEntry> journal, DateTime start, DateTime end)
 {
  if (start.Date > end.Date) throw new ArgumentException("Start date must not exceed end date.");
  var types = accounts.ToDictionary(a => a.Id, a => a.Type);
  decimal revenue = 0m, expenses = 0m;
  foreach (var entry in journal)
  {
   if (!entry.Posted || entry.Date.Date < start.Date || entry.Date.Date > end.Date) continue;
   foreach (var line in entry.Lines)
   {
    if (!types.TryGetValue(line.AccountId, out var type)) continue;
    if (type == "Revenue") revenue += line.Credit - line.Debit;
    else if (type == "Expense") expenses += line.Debit - line.Credit;
   }
  }
  return new PeriodTotals(start.Date, end.Date, revenue, expenses);
 }

 /// <summary>Percent change relative to the magnitude of the previous value; zero baseline is undefined.</summary>
 public static decimal? GrowthPercent(decimal current, decimal previous) =>
  previous == 0m ? null : Math.Round((current - previous) / Math.Abs(previous) * 100m, 2);

 public static string GrowthLabel(decimal current, decimal previous) =>
  GrowthPercent(current, previous) is decimal growth
   ? growth.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) + "%"
   : "N/A";

 public static string ComparePeriodsCsv(PeriodTotals current, PeriodTotals previous)
 {
  static string N(decimal number) => number.ToString(CultureInfo.InvariantCulture);
  static string Date(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
  var csv = new StringBuilder("Period,Start,End,Revenue,Expenses,NetProfit,MarginPercent\r\n");
  foreach (var (label, period) in new[] { ("Selected", current), ("Previous", previous) })
  {
   csv.Append(label).Append(',').Append(Date(period.Start)).Append(',').Append(Date(period.End))
    .Append(',').Append(N(period.Revenue)).Append(',').Append(N(period.Expenses))
    .Append(',').Append(N(period.NetProfit)).Append(',')
    .Append(period.MarginPercent.HasValue ? N(period.MarginPercent.Value) : "N/A").Append("\r\n");
  }
  csv.Append("Change,,,").Append(N(current.Revenue - previous.Revenue))
   .Append(',').Append(N(current.Expenses - previous.Expenses))
   .Append(',').Append(N(current.NetProfit - previous.NetProfit)).Append(",N/A\r\n");
  return csv.ToString();
 }

 public sealed record AccountContribution(int AccountId, string AccountCode, string AccountName,
  string AccountType, decimal Amount);

 /// <summary>Posted-only account contributions in an inclusive period. Reversals retain their sign.</summary>
 public static IReadOnlyList<AccountContribution> BreakdownByAccount(IEnumerable<Account> accounts,
  IEnumerable<JournalEntry> journal, DateTime start, DateTime end, string accountType)
 {
  if (start.Date > end.Date) throw new ArgumentException("Start date must not exceed end date.");
  if (accountType is not ("Revenue" or "Expense"))
   throw new ArgumentException("Only Revenue or Expense is supported.", nameof(accountType));
  var selected = accounts.Where(a => a.Type == accountType).ToDictionary(a => a.Id);
  var totals = new Dictionary<int, decimal>();
  foreach (var entry in journal)
  {
   if (!entry.Posted || entry.Date.Date < start.Date || entry.Date.Date > end.Date) continue;
   foreach (var line in entry.Lines)
   {
    if (!selected.ContainsKey(line.AccountId)) continue;
    var signed = accountType == "Revenue" ? line.Credit - line.Debit : line.Debit - line.Credit;
    totals[line.AccountId] = totals.GetValueOrDefault(line.AccountId) + signed;
   }
  }
  return totals.Select(pair => new AccountContribution(pair.Key, selected[pair.Key].Code,
    selected[pair.Key].Name, accountType, pair.Value))
   .OrderByDescending(row => Math.Abs(row.Amount)).ThenBy(row => row.AccountCode, StringComparer.Ordinal)
   .ToList();
 }

 public static string AccountBreakdownCsv(IEnumerable<AccountContribution> rows)
 {
  static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
  var output = new StringBuilder("AccountCode,AccountName,AccountType,Amount\r\n");
  foreach (var row in rows)
   output.Append(Quote(row.AccountCode)).Append(',').Append(Quote(row.AccountName)).Append(',')
    .Append(Quote(row.AccountType)).Append(',')
    .Append(row.Amount.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
  return output.ToString();
 }

 public static string ToCsv(IEnumerable<MonthSummary> rows)
 {
  var output = new StringBuilder("Period,Revenue,Expenses,NetProfit\r\n");
  foreach (var r in rows)
   output.Append(r.Period).Append(',')
    .Append(r.Revenue.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Expenses.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.NetProfit.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
  return output.ToString();
 }
}
