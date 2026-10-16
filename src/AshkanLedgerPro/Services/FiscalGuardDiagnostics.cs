using System;
using System.Collections.Generic;
using System.Linq;
using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

/// <summary>Read-only inspection of the installed fiscal protection triggers.</summary>
public static class FiscalGuardDiagnostics
{
    private static readonly string[] ProtectedTables =
    {
        "Entries", "Lines", "SalesInvoices", "SalesInvoiceLines",
        "PurchaseInvoices", "PurchaseInvoiceLines", "TreasuryTransactions",
        "Movements", "PaymentChecks", "CheckStatusHistory"
    };

    public sealed record TableStatus(string Table, int TriggerCount, bool Complete);
    public sealed record TriggerDetail(string Table, string Name, bool Present, bool HasLockCondition, bool AbortsWrite);

    /// <summary>Read-only structural verification; checks trigger definitions, not only names.</summary>
    public static List<TriggerDetail> InspectDefinitions()
    {
        using var db = new LedgerDb();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='trigger'";
        db.Database.OpenConnection();
        try
        {
            var definitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = command.ExecuteReader())
                while (reader.Read()) definitions[reader.GetString(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var results = new List<TriggerDetail>();
            foreach (var table in ProtectedTables)
                foreach (var operation in new[] { "insert", "update", "delete" })
                {
                    var prefix = table is "Entries" or "Lines" ? "trg_fiscal_" : "guard_";
                    var name = prefix + table.ToLowerInvariant() + "_" + operation;
                    var present = definitions.TryGetValue(name, out var sql);
                    var hasLock = present && sql!.Contains("FiscalPeriods", StringComparison.OrdinalIgnoreCase)
                        && sql.Contains("Locked", StringComparison.OrdinalIgnoreCase);
                    var aborts = present && sql!.Contains("RAISE(ABORT", StringComparison.OrdinalIgnoreCase);
                    results.Add(new TriggerDetail(table, name, present, hasLock, aborts));
                }
            return results;
        }
        finally { db.Database.CloseConnection(); }
    }

    public static List<TableStatus> Inspect()
    {
        using var db = new LedgerDb();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT tbl_name, name FROM sqlite_master WHERE type='trigger'";
        db.Database.OpenConnection();
        try
        {
            var counts = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var table = reader.GetString(0);
                var name = reader.GetString(1);
                if (!counts.TryGetValue(table, out var names))
                    counts[table] = names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                names.Add(name);
            }
            return ProtectedTables.Select(table =>
            {
                var expected = table is "Entries" or "Lines"
                    ? new[] { "trg_fiscal_" + table.ToLowerInvariant() + "_insert", "trg_fiscal_" + table.ToLowerInvariant() + "_update", "trg_fiscal_" + table.ToLowerInvariant() + "_delete" }
                    : new[] { "guard_" + table.ToLowerInvariant() + "_insert", "guard_" + table.ToLowerInvariant() + "_update", "guard_" + table.ToLowerInvariant() + "_delete" };
                counts.TryGetValue(table, out var installed);
                var present = expected.Count(name => installed?.Contains(name) == true);
                return new TableStatus(table, present, present == expected.Length);
            }).ToList();
        }
        finally { db.Database.CloseConnection(); }
    }
}
