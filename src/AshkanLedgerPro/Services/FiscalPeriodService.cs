using System;
using System.Collections.Generic;
using System.Data;
using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

/// <summary>Explicit, auditable fiscal period locks. Existing ledgers are upgraded non-destructively.</summary>
public sealed class FiscalPeriodService
{
    public sealed record Period(int Id, string Name, DateTime Start, DateTime End, bool Locked);
    public sealed record AuditEvent(int Id, int PeriodId, string Action, string Actor, DateTime OccurredUtc);
    private static void Initialize(LedgerDb db)
    {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS FiscalPeriods (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, StartDate TEXT NOT NULL, EndDate TEXT NOT NULL, Locked INTEGER NOT NULL DEFAULT 0, CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL)");
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS FiscalPeriodAudit (Id INTEGER PRIMARY KEY AUTOINCREMENT, PeriodId INTEGER NOT NULL, Action TEXT NOT NULL, Actor TEXT NOT NULL, OccurredUtc TEXT NOT NULL)");

        // Defense in depth: SQLite triggers protect journal entries and lines even if a caller
        // bypasses the WPF UI or the service-level EnsurePostingAllowed check.
        // EF Core's SQLite DateTime mapping uses ISO-8601 text; substr(...,1,10) is yyyy-MM-dd.
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_fiscal_entries_insert BEFORE INSERT ON Entries
            WHEN EXISTS (SELECT 1 FROM FiscalPeriods WHERE Locked=1 AND StartDate<=substr(NEW.Date,1,10) AND EndDate>=substr(NEW.Date,1,10))
            BEGIN SELECT RAISE(ABORT, 'Fiscal period locked: journal insert denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_fiscal_entries_update BEFORE UPDATE ON Entries
            WHEN EXISTS (SELECT 1 FROM FiscalPeriods WHERE Locked=1 AND
                ((StartDate<=substr(OLD.Date,1,10) AND EndDate>=substr(OLD.Date,1,10)) OR
                 (StartDate<=substr(NEW.Date,1,10) AND EndDate>=substr(NEW.Date,1,10))))
            BEGIN SELECT RAISE(ABORT, 'Fiscal period locked: journal update denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_fiscal_entries_delete BEFORE DELETE ON Entries
            WHEN EXISTS (SELECT 1 FROM FiscalPeriods WHERE Locked=1 AND StartDate<=substr(OLD.Date,1,10) AND EndDate>=substr(OLD.Date,1,10))
            BEGIN SELECT RAISE(ABORT, 'Fiscal period locked: journal delete denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_fiscal_lines_insert BEFORE INSERT ON Lines
            WHEN EXISTS (SELECT 1 FROM Entries e JOIN FiscalPeriods p ON p.Locked=1
                AND p.StartDate<=substr(e.Date,1,10) AND p.EndDate>=substr(e.Date,1,10)
                WHERE e.Id=NEW.JournalEntryId)
            BEGIN SELECT RAISE(ABORT, 'Fiscal period locked: journal line insert denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_fiscal_lines_update BEFORE UPDATE ON Lines
            WHEN EXISTS (SELECT 1 FROM Entries e JOIN FiscalPeriods p ON p.Locked=1
                AND p.StartDate<=substr(e.Date,1,10) AND p.EndDate>=substr(e.Date,1,10)
                WHERE e.Id=OLD.JournalEntryId OR e.Id=NEW.JournalEntryId)
            BEGIN SELECT RAISE(ABORT, 'Fiscal period locked: journal line update denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_fiscal_lines_delete BEFORE DELETE ON Lines
            WHEN EXISTS (SELECT 1 FROM Entries e JOIN FiscalPeriods p ON p.Locked=1
                AND p.StartDate<=substr(e.Date,1,10) AND p.EndDate>=substr(e.Date,1,10)
                WHERE e.Id=OLD.JournalEntryId)
            BEGIN SELECT RAISE(ABORT, 'Fiscal period locked: journal line delete denied'); END
            """);

    }
    public List<Period> List()
    {
        using var db = new LedgerDb(); Initialize(db);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT Id,Name,StartDate,EndDate,Locked FROM FiscalPeriods ORDER BY StartDate DESC";
        cmd.Connection!.Open();
        using var reader = cmd.ExecuteReader();
        var rows = new List<Period>();
        while(reader.Read()) rows.Add(new Period(reader.GetInt32(0), reader.GetString(1), DateTime.ParseExact(reader.GetString(2), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), reader.GetInt32(4) != 0));
        return rows;
    }
    public List<AuditEvent> AuditTrail(int? periodId = null)
    {
        using var db = new LedgerDb(); Initialize(db);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = periodId.HasValue
            ? "SELECT Id,PeriodId,Action,Actor,OccurredUtc FROM FiscalPeriodAudit WHERE PeriodId=@periodId ORDER BY Id DESC LIMIT 500"
            : "SELECT Id,PeriodId,Action,Actor,OccurredUtc FROM FiscalPeriodAudit ORDER BY Id DESC LIMIT 500";
        if (periodId.HasValue)
        {
            var parameter = cmd.CreateParameter(); parameter.ParameterName = "@periodId";
            parameter.Value = periodId.Value; cmd.Parameters.Add(parameter);
        }
        cmd.Connection!.Open();
        using var reader = cmd.ExecuteReader();
        var result = new List<AuditEvent>();
        while (reader.Read())
            result.Add(new AuditEvent(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                DateTime.Parse(reader.GetString(4), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)));
        return result;
    }
    public void Create(string name, DateTime start, DateTime end, string actor)
    {
        if(string.IsNullOrWhiteSpace(name) || name.Length > 100 || start.Date > end.Date) throw new ArgumentException("Invalid fiscal period.");
        using var db = new LedgerDb(); Initialize(db);
        using var tx = db.Database.BeginTransaction();
        var overlaps = db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM FiscalPeriods WHERE StartDate <= {0} AND EndDate >= {1}", end.ToString("yyyy-MM-dd"), start.ToString("yyyy-MM-dd")).Single();
        if(overlaps > 0) throw new InvalidOperationException("Fiscal periods must not overlap.");
        db.Database.ExecuteSqlInterpolated($"INSERT INTO FiscalPeriods (Name,StartDate,EndDate,Locked,CreatedUtc,UpdatedUtc) VALUES ({name.Trim()},{start.ToString("yyyy-MM-dd")},{end.ToString("yyyy-MM-dd")},0,{DateTime.UtcNow.ToString("O")},{DateTime.UtcNow.ToString("O")})");
        db.Database.ExecuteSqlInterpolated($"INSERT INTO FiscalPeriodAudit (PeriodId,Action,Actor,OccurredUtc) VALUES (last_insert_rowid(),{"Create"},{actor},{DateTime.UtcNow.ToString("O")})");
        tx.Commit();
    }
    public void SetLocked(int id, bool locked, string actor)
    {
        using var db = new LedgerDb(); Initialize(db);
        using var tx = db.Database.BeginTransaction();
        var count = db.Database.ExecuteSqlInterpolated($"UPDATE FiscalPeriods SET Locked = {(locked ? 1 : 0)}, UpdatedUtc = {DateTime.UtcNow.ToString("O")} WHERE Id = {id}");
        if(count != 1) throw new ArgumentException("Fiscal period not found.");
        db.Database.ExecuteSqlInterpolated($"INSERT INTO FiscalPeriodAudit (PeriodId,Action,Actor,OccurredUtc) VALUES ({id},{(locked ? "Lock" : "Unlock")},{actor},{DateTime.UtcNow.ToString("O")})");
        tx.Commit();
    }
    public static void EnsurePostingAllowed(LedgerDb db, DateTime date)
    {
        Initialize(db);
        var day = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if(db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM FiscalPeriods WHERE Locked=1 AND StartDate <= {0} AND EndDate >= {0}", day).Single() > 0)
            throw new InvalidOperationException("This fiscal period is locked. Posting is not allowed.");
    }
}
