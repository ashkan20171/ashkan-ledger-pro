using AshkanLedgerPro.Models;
namespace AshkanLedgerPro.Services;

/// <summary>Read-only financial reminders. No automatic changes to checks or ledger entries.</summary>
public static class FinancialAlertService
{
 public sealed record Alert(string Category, string Reference, DateTime DueDate, decimal Amount, int DaysFromToday);
 public static IReadOnlyList<Alert> CheckAlerts(IEnumerable<PaymentCheck> checks, DateTime today, int upcomingDays = 14)
 {
  if(upcomingDays < 0 || upcomingDays > 365) throw new ArgumentOutOfRangeException(nameof(upcomingDays));
  var date = today.Date;
  return checks.Where(c => c.Status == "Pending" && c.DueDate.Date <= date.AddDays(upcomingDays))
   .Select(c => new Alert(c.DueDate.Date < date ? "Overdue" : c.DueDate.Date == date ? "Today" : "Upcoming",
     c.Number, c.DueDate.Date, c.Amount, (c.DueDate.Date - date).Days))
   .OrderBy(a => a.DueDate).ThenBy(a => a.Reference, StringComparer.OrdinalIgnoreCase).ToList();
 }
}
