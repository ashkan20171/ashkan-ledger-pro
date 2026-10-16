using AshkanLedgerPro.Models;
using System.Globalization;
using System.Text;

namespace AshkanLedgerPro.Services;

/// <summary>Cash movement report based on recorded treasury transactions, not accrual profit.</summary>
public static class CashFlowAnalyticsService
{
 public sealed record MonthCashFlow(int Year, int Month, decimal Receipts, decimal Payments,
  decimal CashReceipts, decimal CashPayments, decimal BankReceipts, decimal BankPayments)
 {
  public decimal Net => Receipts - Payments;
  public string Period => $"{Year:D4}-{Month:D2}";
 }

 public static IReadOnlyList<MonthCashFlow> ForYear(IEnumerable<TreasuryTransaction> transactions, int year)
 {
  if (year < 1900 || year > 9998) throw new ArgumentOutOfRangeException(nameof(year));
  var totals = new (decimal CR, decimal CP, decimal BR, decimal BP)[12];
  foreach (var t in transactions)
  {
   if (t.OccurredAt.Year != year) continue;
   if (t.Amount < 0) throw new InvalidOperationException("Negative treasury transaction amount.");
   int i = t.OccurredAt.Month - 1;
   if (t.Kind == "Receipt" && t.Channel == "Cash") totals[i].CR += t.Amount;
   else if (t.Kind == "Payment" && t.Channel == "Cash") totals[i].CP += t.Amount;
   else if (t.Kind == "Receipt" && t.Channel == "Bank") totals[i].BR += t.Amount;
   else if (t.Kind == "Payment" && t.Channel == "Bank") totals[i].BP += t.Amount;
   // Unknown categories are excluded, not silently classified as cash.
  }
  return Enumerable.Range(1,12).Select(month => {
   var t = totals[month - 1];
   return new MonthCashFlow(year,month,t.CR+t.BR,t.CP+t.BP,t.CR,t.CP,t.BR,t.BP);
  }).ToList();
 }

 public sealed record LiquidityTrend(int Month, decimal Receipts, decimal Payments, decimal Net, decimal CumulativeNet, bool BelowThreshold);

 /// <summary>Running movement, not an account balance: excludes opening cash and unrecorded bank movements.</summary>
 public static IReadOnlyList<LiquidityTrend> Trend(IEnumerable<MonthCashFlow> months, decimal openingMovement = 0m, decimal warningThreshold = 0m)
 {
  var ordered = months.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();
  if (ordered.Select(m => (m.Year, m.Month)).Distinct().Count() != ordered.Count)
   throw new ArgumentException("Duplicate cash-flow months.", nameof(months));
  decimal running = openingMovement;
  return ordered.Select(m => {
   running += m.Net;
   return new LiquidityTrend(m.Month, m.Receipts, m.Payments, m.Net, running, running < warningThreshold);
  }).ToList();
 }

 public static string TrendCsv(IEnumerable<LiquidityTrend> rows)
 {
  var sb = new StringBuilder("Month,Receipts,Payments,Net,CumulativeNet,BelowThreshold\r\n");
  foreach (var r in rows)
   sb.Append(r.Month.ToString("D2", CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Receipts.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Payments.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Net.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.CumulativeNet.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.BelowThreshold ? "true" : "false").Append("\r\n");
  return sb.ToString();
 }

 public static string ToCsv(IEnumerable<MonthCashFlow> rows)
 {
  var sb = new StringBuilder("Period,Receipts,Payments,Net,CashReceipts,CashPayments,BankReceipts,BankPayments\r\n");
  foreach(var r in rows)
  {
   sb.Append(r.Period);
   foreach(var n in new[]{r.Receipts,r.Payments,r.Net,r.CashReceipts,r.CashPayments,r.BankReceipts,r.BankPayments})
    sb.Append(',').Append(n.ToString(CultureInfo.InvariantCulture));
   sb.Append("\r\n");
  }
  return sb.ToString();
 }
}
