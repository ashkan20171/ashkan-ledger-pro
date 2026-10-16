using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only account movement and opening/closing balances. Debit-positive convention.</summary>
public sealed class FinancialPeriodReportService
{
    public sealed record BalanceRow(string Code, string Name, string Type,
        decimal Opening, decimal Debit, decimal Credit, decimal Closing)
    {
        public decimal OpeningDebit => Math.Max(0m, Opening);
        public decimal OpeningCredit => Math.Max(0m, -Opening);
        public decimal ClosingDebit => Math.Max(0m, Closing);
        public decimal ClosingCredit => Math.Max(0m, -Closing);
    }

    public sealed record JournalAudit(int EntryCount, int UnbalancedCount, decimal TotalDebit, decimal TotalCredit);

    /// <summary>Read-only validation of posted entries in the inclusive date range.</summary>
    public JournalAudit ValidatePostedEntries(DateTime from, DateTime to)
    {
        if (from.Date > to.Date) throw new ArgumentException("Start date must not exceed end date.");
        if (to.Date == DateTime.MaxValue.Date) throw new ArgumentOutOfRangeException(nameof(to));
        using var db = new LedgerDb();
        var entries = db.Entries.AsNoTracking().Include(e => e.Lines)
            .Where(e => e.Posted && e.Date >= from.Date && e.Date < to.Date.AddDays(1)).ToList();
        var debit = 0m;
        var credit = 0m;
        var invalid = 0;
        foreach (var entry in entries)
        {
            var d = entry.Lines.Sum(line => line.Debit);
            var c = entry.Lines.Sum(line => line.Credit);
            debit += d;
            credit += c;
            if (d != c || d <= 0m || entry.Lines.Count < 2) invalid++;
        }
        return new JournalAudit(entries.Count, invalid, debit, credit);
    }

    public IReadOnlyList<BalanceRow> GetBalances(DateTime from, DateTime to)
    {
        if (from.Date > to.Date) throw new ArgumentException("Start date must not exceed end date.");
        if (to.Date == DateTime.MaxValue.Date) throw new ArgumentOutOfRangeException(nameof(to));
        using var db = new LedgerDb();
        var accounts = db.Accounts.AsNoTracking().OrderBy(a => a.Code).ToList();
        // Query posted entries only. End is exclusive, so the entire last day is included.
        var lines = db.Entries.AsNoTracking().Include(e => e.Lines)
            .Where(e => e.Posted && e.Date < to.Date.AddDays(1))
            .SelectMany(e => e.Lines.Select(l => new { e.Date, l.AccountId, l.Debit, l.Credit }))
            .ToList();
        var sums = lines.GroupBy(x => x.AccountId).ToDictionary(g => g.Key, g => new
        {
            Opening = g.Where(x => x.Date < from.Date).Sum(x => x.Debit - x.Credit),
            Debit = g.Where(x => x.Date >= from.Date).Sum(x => x.Debit),
            Credit = g.Where(x => x.Date >= from.Date).Sum(x => x.Credit)
        });
        return accounts.Select(a =>
        {
            if (!sums.TryGetValue(a.Id, out var total))
                return new BalanceRow(a.Code, a.Name, a.Type, 0m, 0m, 0m, 0m);
            return new BalanceRow(a.Code, a.Name, a.Type,
                total.Opening, total.Debit, total.Credit, total.Opening + total.Debit - total.Credit);
        }).ToList();
    }
}
