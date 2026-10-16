using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only general ledger for a single account, with opening and running balances.</summary>
public sealed class AccountLedgerReportService
{
    public sealed record LedgerRow(int EntryId, DateTime Date, string Description,
        decimal Debit, decimal Credit, decimal RunningBalance);
    public sealed record LedgerResult(decimal OpeningBalance, decimal PeriodDebit,
        decimal PeriodCredit, decimal ClosingBalance, IReadOnlyList<LedgerRow> Rows);

    public LedgerResult GetAccountLedger(int accountId, DateTime from, DateTime to)
    {
        if (from.Date > to.Date) throw new ArgumentException("Invalid date range.");
        if (to.Date == DateTime.MaxValue.Date) throw new ArgumentOutOfRangeException(nameof(to));
        using var db = new LedgerDb();
        if (!db.Accounts.AsNoTracking().Any(a => a.Id == accountId))
            throw new ArgumentException("Account not found.", nameof(accountId));
        // Materialize first so decimal arithmetic is performed by .NET rather than SQLite floating point.
        var entries = db.Entries.AsNoTracking().Include(e => e.Lines)
            .Where(e => e.Posted && e.Date < to.Date.AddDays(1))
            .OrderBy(e => e.Date).ThenBy(e => e.Id).ToList();
        var lines = entries.SelectMany(e => e.Lines.Where(l => l.AccountId == accountId)
            .Select(l => new { EntryId = e.Id, e.Date, e.Description, LineId = l.Id, l.Debit, l.Credit }))
            .OrderBy(x => x.Date).ThenBy(x => x.EntryId).ThenBy(x => x.LineId).ToList();
        decimal opening = lines.Where(x => x.Date < from.Date).Sum(x => x.Debit - x.Credit);
        decimal running = opening;
        var result = new List<LedgerRow>();
        foreach (var line in lines.Where(x => x.Date >= from.Date))
        {
            running += line.Debit - line.Credit;
            result.Add(new LedgerRow(line.EntryId, line.Date, line.Description,
                line.Debit, line.Credit, running));
        }
        return new LedgerResult(opening, result.Sum(x => x.Debit),
            result.Sum(x => x.Credit), running, result);
    }
}
