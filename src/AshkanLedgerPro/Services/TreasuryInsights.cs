using AshkanLedgerPro.Models;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only treasury indicators; never creates journal entries.</summary>
public static class TreasuryInsights
{
    public sealed record Summary(decimal CashReceipts, decimal CashPayments,
        decimal BankReceipts, decimal BankPayments, decimal NetCashFlow,
        int OverdueChecks, decimal PendingReceivedChecks, decimal PendingIssuedChecks);

    public static Summary Calculate(IEnumerable<TreasuryTransaction> transactions,
        IEnumerable<PaymentCheck> checks, DateTime asOfDate)
    {
        var items = transactions.ToList();
        var pending = checks.Where(c => c.Status == "Pending").ToList();
        decimal Sum(string kind, string channel) => items
            .Where(t => t.Kind == kind && t.Channel == channel).Sum(t => t.Amount);
        var cr = Sum("Receipt", "Cash");
        var cp = Sum("Payment", "Cash");
        var br = Sum("Receipt", "Bank");
        var bp = Sum("Payment", "Bank");
        return new Summary(cr, cp, br, bp, cr + br - cp - bp,
            pending.Count(c => c.DueDate.Date < asOfDate.Date),
            pending.Where(c => c.Direction == "Received").Sum(c => c.Amount),
            pending.Where(c => c.Direction == "Issued").Sum(c => c.Amount));
    }
}
