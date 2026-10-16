using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AshkanLedgerPro.Services;

/// <summary>Explicit allocation of an existing treasury receipt/payment to an invoice.
/// Allocation is a reconciliation record, not a second financial posting.</summary>
public static class InvoiceAllocationService
{
    public sealed record Allocation(int Id, int TreasuryTransactionId, string InvoiceKind,
        int InvoiceId, decimal Amount, DateTime AllocatedAtUtc);
    public sealed record Outstanding(string Kind, int InvoiceId, int PartnerId, DateTime InvoiceDate,
        decimal Total, decimal Allocated, decimal Remaining, int AgeDays, DateTime DueDate, int DaysPastDue, string AgingBucket);

    public static void Initialize(LedgerDb db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS InvoiceAllocations (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              TreasuryTransactionId INTEGER NOT NULL REFERENCES TreasuryTransactions(Id) ON DELETE RESTRICT,
              InvoiceKind TEXT NOT NULL CHECK(InvoiceKind IN ('Sales','Purchase')),
              InvoiceId INTEGER NOT NULL,
              Amount TEXT NOT NULL,
              AllocatedAtUtc TEXT NOT NULL
            )
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS InvoiceAllocationReversals (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              AllocationId INTEGER NOT NULL UNIQUE REFERENCES InvoiceAllocations(Id) ON DELETE RESTRICT,
              Reason TEXT NOT NULL CHECK(length(trim(Reason)) >= 8),
              ReversedBy TEXT NOT NULL,
              ReversedAtUtc TEXT NOT NULL
            )
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_allocation_reversals_no_update
            BEFORE UPDATE ON InvoiceAllocationReversals
            BEGIN SELECT RAISE(ABORT,'Reversal audit is immutable'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_allocation_reversals_no_delete
            BEFORE DELETE ON InvoiceAllocationReversals
            BEGIN SELECT RAISE(ABORT,'Reversal audit is immutable'); END
            """);
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InvoiceAllocations_Invoice ON InvoiceAllocations(InvoiceKind,InvoiceId)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_InvoiceAllocations_Treasury ON InvoiceAllocations(TreasuryTransactionId)");
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS InvoiceDueDates (
              InvoiceKind TEXT NOT NULL CHECK(InvoiceKind IN ('Sales','Purchase')),
              InvoiceId INTEGER NOT NULL,
              DueDate TEXT NOT NULL,
              UpdatedAtUtc TEXT NOT NULL,
              PRIMARY KEY (InvoiceKind, InvoiceId)
            )
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS InvoiceDueDateAudit (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              InvoiceKind TEXT NOT NULL,
              InvoiceId INTEGER NOT NULL,
              PreviousDueDate TEXT NULL,
              NewDueDate TEXT NOT NULL,
              ChangedAtUtc TEXT NOT NULL,
              ChangedBy TEXT NOT NULL DEFAULT 'unknown'
            )
            """);
        // Upgrade existing Stage 59 databases without destroying historical audit rows.
        using (var schema = db.Database.GetDbConnection().CreateCommand())
        {
            if (schema.Connection!.State != System.Data.ConnectionState.Open) schema.Connection.Open();
            schema.CommandText = "PRAGMA table_info(InvoiceDueDateAudit)";
            bool hasActor = false;
            using (var reader = schema.ExecuteReader())
                while (reader.Read()) if (string.Equals(reader.GetString(1), "ChangedBy", StringComparison.OrdinalIgnoreCase)) hasActor = true;
            if (!hasActor) db.Database.ExecuteSqlRaw("ALTER TABLE InvoiceDueDateAudit ADD COLUMN ChangedBy TEXT NOT NULL DEFAULT 'unknown'");
        }
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_due_audit_no_update BEFORE UPDATE ON InvoiceDueDateAudit
            BEGIN SELECT RAISE(ABORT,'Due-date audit records cannot be modified'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_due_audit_no_delete BEFORE DELETE ON InvoiceDueDateAudit
            BEGIN SELECT RAISE(ABORT,'Due-date audit records cannot be deleted'); END
            """);
        // Prevent edits that could silently invalidate reconciled balances.
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_allocations_no_update BEFORE UPDATE ON InvoiceAllocations
            BEGIN SELECT RAISE(ABORT,'Allocations are immutable; create a reversal workflow'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS trg_allocations_no_delete BEFORE DELETE ON InvoiceAllocations
            BEGIN SELECT RAISE(ABORT,'Allocations are immutable; create a reversal workflow'); END
            """);
    }

    public static List<Allocation> List(LedgerDb db)
    {
        Initialize(db);
        using var cmd=db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText="SELECT a.Id,a.TreasuryTransactionId,a.InvoiceKind,a.InvoiceId,a.Amount,a.AllocatedAtUtc FROM InvoiceAllocations a WHERE NOT EXISTS (SELECT 1 FROM InvoiceAllocationReversals r WHERE r.AllocationId=a.Id) ORDER BY a.Id DESC";
        if(cmd.Connection!.State!=System.Data.ConnectionState.Open)cmd.Connection.Open();
        using var reader=cmd.ExecuteReader();
        var result=new List<Allocation>();
        while(reader.Read()) result.Add(new Allocation(reader.GetInt32(0),reader.GetInt32(1),reader.GetString(2),
            reader.GetInt32(3),decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture),
            DateTime.Parse(reader.GetString(5),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind)));
        return result;
    }

    public sealed record Reversal(int Id, int AllocationId, string Reason, string ReversedBy, string ReversedAtUtc);

    /// <summary>Reverses reconciliation only; no treasury transaction or journal entry is deleted.</summary>
    public static int Reverse(LedgerDb db, int allocationId, string reason, string actor)
    {
        if (allocationId <= 0) throw new ArgumentOutOfRangeException(nameof(allocationId));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 8 || reason.Length > 1000)
            throw new ArgumentException("A meaningful reason (8-1000 characters) is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 128)
            throw new ArgumentException("Authenticated user is required.", nameof(actor));
        Initialize(db);
        using var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
        FiscalPeriodService.EnsurePostingAllowed(db, DateTime.Today);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = tx.GetDbTransaction();
        cmd.CommandText = """
            INSERT INTO InvoiceAllocationReversals(AllocationId,Reason,ReversedBy,ReversedAtUtc)
            SELECT Id,$reason,$actor,$utc FROM InvoiceAllocations
            WHERE Id=$id AND NOT EXISTS
              (SELECT 1 FROM InvoiceAllocationReversals WHERE AllocationId=$id);
            SELECT changes();
            """;
        void Add(string key, object value) { var p = cmd.CreateParameter(); p.ParameterName = key; p.Value = value; cmd.Parameters.Add(p); }
        Add("$id", allocationId); Add("$reason", reason.Trim()); Add("$actor", actor);
        Add("$utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        var changed = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (changed != 1) throw new InvalidOperationException("Allocation does not exist or is already reversed.");
        tx.Commit();
        return allocationId;
    }

    public static IReadOnlyList<Reversal> ReversalHistory(LedgerDb db, int limit = 200)
    {
        Initialize(db);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT Id,AllocationId,Reason,ReversedBy,ReversedAtUtc FROM InvoiceAllocationReversals ORDER BY Id DESC LIMIT $limit";
        var p = cmd.CreateParameter(); p.ParameterName = "$limit"; p.Value = Math.Clamp(limit, 1, 1000); cmd.Parameters.Add(p);
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        using var reader = cmd.ExecuteReader();
        var result = new List<Reversal>();
        while (reader.Read()) result.Add(new Reversal(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return result;
    }

    /// <summary>Read-only, chronological audit of allocation creation and optional reversal.
    /// Historical amounts are shown as recorded; this is not an as-of invoice balance.</summary>
    public sealed record AllocationAuditRow(int AllocationId, string InvoiceKind, int InvoiceId,
        int TreasuryTransactionId, decimal Amount, string AllocatedAtUtc,
        string Status, string? ReversedAtUtc, string? ReversedBy, string? ReversalReason);

    public static IReadOnlyList<AllocationAuditRow> AllocationAudit(LedgerDb db, int limit = 1000)
    {
        Initialize(db);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = """
            SELECT a.Id,a.InvoiceKind,a.InvoiceId,a.TreasuryTransactionId,a.Amount,a.AllocatedAtUtc,
                   r.ReversedAtUtc,r.ReversedBy,r.Reason
            FROM InvoiceAllocations a
            LEFT JOIN InvoiceAllocationReversals r ON r.AllocationId=a.Id
            ORDER BY a.Id DESC LIMIT $limit
            """;
        var limitParam = cmd.CreateParameter(); limitParam.ParameterName = "$limit";
        limitParam.Value = Math.Clamp(limit, 1, 10000); cmd.Parameters.Add(limitParam);
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        using var reader = cmd.ExecuteReader();
        var result = new List<AllocationAuditRow>();
        while (reader.Read())
        {
            string? reversedAt = reader.IsDBNull(6) ? null : reader.GetString(6);
            result.Add(new AllocationAuditRow(reader.GetInt32(0),reader.GetString(1),reader.GetInt32(2),
                reader.GetInt32(3),decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture),
                reader.GetString(5),reversedAt is null ? "Active" : "Reversed",reversedAt,
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8)));
        }
        return result;
    }

    /// <summary>Historical allocation balances as at an exact UTC instant. An allocation
    /// reversed after the cutoff still counts at the cutoff. No mutation occurs.</summary>
    public sealed record HistoricalInvoiceBalance(string InvoiceKind, int InvoiceId, int PartnerId,
        DateTime InvoiceDateUtc, decimal InvoiceTotal, decimal AllocatedAsOf, decimal RemainingAsOf);

    public static IReadOnlyList<HistoricalInvoiceBalance> HistoricalBalances(LedgerDb db, DateTime asOfUtc)
    {
        Initialize(db);
        var cutoff = asOfUtc.Kind == DateTimeKind.Utc ? asOfUtc : asOfUtc.ToUniversalTime();
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = """
            SELECT a.InvoiceKind,a.InvoiceId,a.Amount,a.AllocatedAtUtc,r.ReversedAtUtc
            FROM InvoiceAllocations a
            LEFT JOIN InvoiceAllocationReversals r ON r.AllocationId=a.Id
            """;
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        var paid = new Dictionary<(string,int),decimal>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var allocatedAt = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                if (allocatedAt > cutoff) continue;
                if (!reader.IsDBNull(4))
                {
                    var reversedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    if (reversedAt <= cutoff) continue;
                }
                var key = (reader.GetString(0),reader.GetInt32(1));
                paid[key] = paid.GetValueOrDefault(key) + decimal.Parse(reader.GetString(2),CultureInfo.InvariantCulture);
            }
        }
        var result = new List<HistoricalInvoiceBalance>();
        void Add(string kind,int id,int partner,DateTime issued,decimal total)
        {
            var issueUtc = issued.Kind == DateTimeKind.Utc ? issued : issued.ToUniversalTime();
            if (issueUtc > cutoff) return;
            var applied = paid.GetValueOrDefault((kind,id));
            result.Add(new HistoricalInvoiceBalance(kind,id,partner,issueUtc,total,applied,total-applied));
        }
        foreach (var invoice in db.SalesInvoices.AsNoTracking().ToList())
            Add("Sales",invoice.Id,invoice.PartnerId,invoice.CreatedAtUtc,invoice.Total);
        foreach (var invoice in db.PurchaseInvoices.AsNoTracking().ToList())
            Add("Purchase",invoice.Id,invoice.PartnerId,invoice.CreatedAtUtc,invoice.Total);
        return result.OrderBy(x=>x.InvoiceKind).ThenBy(x=>x.InvoiceId).ToList();
    }

    public static int Allocate(LedgerDb db,int treasuryId,string kind,int invoiceId,decimal amount)
    {
        if(kind is not ("Sales" or "Purchase"))throw new ArgumentException("Invalid invoice kind.");
        if(amount<=0 || decimal.Round(amount,2)!=amount)throw new ArgumentException("Amount must be positive with at most 2 decimal places.");
        Initialize(db);
        using var tx=db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var treasury=db.TreasuryTransactions.AsNoTracking().SingleOrDefault(x=>x.Id==treasuryId)
            ?? throw new ArgumentException("Treasury transaction not found.");
        int partnerId; decimal invoiceTotal; DateTime invoiceDate;
        if(kind=="Sales")
        {
            var invoice=db.SalesInvoices.AsNoTracking().SingleOrDefault(x=>x.Id==invoiceId)
                ?? throw new ArgumentException("Sales invoice not found.");
            partnerId=invoice.PartnerId;invoiceTotal=invoice.Total;invoiceDate=invoice.CreatedAtUtc;
            if(treasury.Kind!="Receipt")throw new InvalidOperationException("Sales invoices require receipts.");
        }
        else
        {
            var invoice=db.PurchaseInvoices.AsNoTracking().SingleOrDefault(x=>x.Id==invoiceId)
                ?? throw new ArgumentException("Purchase invoice not found.");
            partnerId=invoice.PartnerId;invoiceTotal=invoice.Total;invoiceDate=invoice.CreatedAtUtc;
            if(treasury.Kind!="Payment")throw new InvalidOperationException("Purchase invoices require payments.");
        }
        if(treasury.PartnerId!=partnerId)throw new InvalidOperationException("Invoice and treasury partner must match.");
        if(treasury.OccurredAt<invoiceDate)throw new InvalidOperationException("Treasury transaction predates invoice; manual review required.");
        // Reconciliation of already posted documents is allowed in an open period only.
        FiscalPeriodService.EnsurePostingAllowed(db,DateTime.Today);
        var existing=List(db);
        var usedTreasury=existing.Where(x=>x.TreasuryTransactionId==treasuryId).Sum(x=>x.Amount);
        var usedInvoice=existing.Where(x=>x.InvoiceKind==kind&&x.InvoiceId==invoiceId).Sum(x=>x.Amount);
        if(amount>treasury.Amount-usedTreasury)throw new InvalidOperationException("Allocation exceeds unallocated treasury amount.");
        if(amount>invoiceTotal-usedInvoice)throw new InvalidOperationException("Allocation exceeds invoice outstanding amount.");
        using var cmd=db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction=tx.GetDbTransaction();
        cmd.CommandText="INSERT INTO InvoiceAllocations(TreasuryTransactionId,InvoiceKind,InvoiceId,Amount,AllocatedAtUtc) VALUES ($treasury,$kind,$invoice,$amount,$utc); SELECT last_insert_rowid();";
        void Add(string key,object value){var p=cmd.CreateParameter();p.ParameterName=key;p.Value=value;cmd.Parameters.Add(p);}
        Add("$treasury",treasuryId);Add("$kind",kind);Add("$invoice",invoiceId);
        Add("$amount",amount.ToString("0.00",CultureInfo.InvariantCulture));Add("$utc",DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture));
        var id=Convert.ToInt32(cmd.ExecuteScalar(),CultureInfo.InvariantCulture);
        tx.Commit();return id;
    }

    /// <summary>Sets an explicit contractual due date. Existing invoice records are not rewritten.</summary>
    public static void SetDueDate(LedgerDb db, string kind, int invoiceId, DateTime dueDate, string changedBy = "unknown")
    {
        if (kind is not ("Sales" or "Purchase")) throw new ArgumentException("Invalid invoice kind.");
        if (string.IsNullOrWhiteSpace(changedBy) || changedBy.Length > 128)
            throw new ArgumentException("Invalid audit actor.", nameof(changedBy));
        Initialize(db);
        var issued = kind == "Sales"
            ? db.SalesInvoices.AsNoTracking().Where(x => x.Id == invoiceId).Select(x => (DateTime?)x.CreatedAtUtc).SingleOrDefault()
            : db.PurchaseInvoices.AsNoTracking().Where(x => x.Id == invoiceId).Select(x => (DateTime?)x.CreatedAtUtc).SingleOrDefault();
        if (issued is null) throw new ArgumentException("Invoice not found.");
        if (dueDate.Date < issued.Value.Date) throw new ArgumentException("Due date cannot precede invoice date.");
        FiscalPeriodService.EnsurePostingAllowed(db, DateTime.Today);
        using var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = tx.GetDbTransaction();
        cmd.CommandText = """
            INSERT INTO InvoiceDueDates(InvoiceKind,InvoiceId,DueDate,UpdatedAtUtc)
            VALUES($kind,$id,$date,$utc)
            ON CONFLICT(InvoiceKind,InvoiceId) DO UPDATE SET
              DueDate=excluded.DueDate, UpdatedAtUtc=excluded.UpdatedAtUtc
            """;
        void Add(string key, object value) { var parameter = cmd.CreateParameter(); parameter.ParameterName = key; parameter.Value = value; cmd.Parameters.Add(parameter); }
        Add("$kind", kind); Add("$id", invoiceId);
        Add("$date", dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("$utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        // Record the previous value atomically with the update; no audit entry for a no-op.
        cmd.CommandText = "SELECT DueDate FROM InvoiceDueDates WHERE InvoiceKind=$kind AND InvoiceId=$id";
        var previous = cmd.ExecuteScalar() as string;
        var newValue = dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (previous == newValue) { tx.Commit(); return; }
        cmd.CommandText = """
            INSERT INTO InvoiceDueDates(InvoiceKind,InvoiceId,DueDate,UpdatedAtUtc)
            VALUES($kind,$id,$date,$utc)
            ON CONFLICT(InvoiceKind,InvoiceId) DO UPDATE SET
              DueDate=excluded.DueDate, UpdatedAtUtc=excluded.UpdatedAtUtc
            """;
        cmd.ExecuteNonQuery();
        using var audit = db.Database.GetDbConnection().CreateCommand();
        audit.Transaction = tx.GetDbTransaction();
        audit.CommandText = "INSERT INTO InvoiceDueDateAudit(InvoiceKind,InvoiceId,PreviousDueDate,NewDueDate,ChangedAtUtc,ChangedBy) VALUES($kind,$id,$old,$new,$utc,$actor)";
        void AuditParam(string name, object value) { var param = audit.CreateParameter(); param.ParameterName=name;param.Value=value;audit.Parameters.Add(param); }
        AuditParam("$kind",kind);AuditParam("$id",invoiceId);AuditParam("$old",previous is null ? DBNull.Value : previous);
        AuditParam("$new",newValue);AuditParam("$utc",DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture));
        AuditParam("$actor",changedBy.Trim());
        audit.ExecuteNonQuery();
        tx.Commit();
    }

    public sealed record DueDateChange(int Id, string InvoiceKind, int InvoiceId, string? PreviousDueDate, string NewDueDate, string ChangedAtUtc, string ChangedBy);
    public static IReadOnlyList<DueDateChange> DueDateHistory(LedgerDb db, int limit=200)
    {
        Initialize(db);
        using var cmd=db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText="SELECT Id,InvoiceKind,InvoiceId,PreviousDueDate,NewDueDate,ChangedAtUtc,ChangedBy FROM InvoiceDueDateAudit ORDER BY Id DESC LIMIT $limit";
        var parameter=cmd.CreateParameter();parameter.ParameterName="$limit";parameter.Value=Math.Clamp(limit,1,1000);cmd.Parameters.Add(parameter);
        if(cmd.Connection!.State!=System.Data.ConnectionState.Open)cmd.Connection.Open();
        using var reader=cmd.ExecuteReader();
        var result=new List<DueDateChange>();
        while(reader.Read())result.Add(new DueDateChange(reader.GetInt32(0),reader.GetString(1),reader.GetInt32(2),
            reader.IsDBNull(3)?null:reader.GetString(3),reader.GetString(4),reader.GetString(5),reader.GetString(6)));
        return result;
    }

    private static Dictionary<(string Kind, int InvoiceId), DateTime> LoadDueDates(LedgerDb db)
    {
        Initialize(db);
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT InvoiceKind,InvoiceId,DueDate FROM InvoiceDueDates";
        if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
        using var reader = cmd.ExecuteReader();
        var result = new Dictionary<(string, int), DateTime>();
        while (reader.Read()) result[(reader.GetString(0), reader.GetInt32(1))] =
            DateTime.ParseExact(reader.GetString(2), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return result;
    }

    public static IReadOnlyList<Outstanding> OutstandingInvoices(LedgerDb db,DateTime asOf)
    {
        var dueDates = LoadDueDates(db);
        var allocations=List(db).Where(x=>x.AllocatedAtUtc.Date<=asOf.Date).ToList();
        var cutoff=asOf.Date.AddDays(1);
        var result=new List<Outstanding>();
        void Add(string kind,int id,int partner,DateTime date,decimal total)
        {
            if(date>=cutoff)return;
            var paid=allocations.Where(x=>x.InvoiceKind==kind&&x.InvoiceId==id).Sum(x=>x.Amount);
            // Without a contractual due date, age is unknown: do not label the invoice overdue.
            var hasDueDate = dueDates.TryGetValue((kind, id), out var dueDate);
            var daysPastDue = hasDueDate ? Math.Max(0, (asOf.Date - dueDate.Date).Days) : 0;
            var bucket = !hasDueDate ? "Due date missing" : total - paid <= 0 ? "Settled"
                : daysPastDue == 0 ? "Current" : daysPastDue <= 30 ? "1-30"
                : daysPastDue <= 60 ? "31-60" : daysPastDue <= 90 ? "61-90" : "90+";
            result.Add(new Outstanding(kind,id,partner,date,total,paid,total-paid,
                Math.Max(0,(asOf.Date-date.Date).Days),hasDueDate ? dueDate : DateTime.MinValue,daysPastDue,bucket));
        }
        foreach(var x in db.SalesInvoices.AsNoTracking().ToList())Add("Sales",x.Id,x.PartnerId,x.CreatedAtUtc,x.Total);
        foreach(var x in db.PurchaseInvoices.AsNoTracking().ToList())Add("Purchase",x.Id,x.PartnerId,x.CreatedAtUtc,x.Total);
        return result.OrderByDescending(x=>x.AgeDays).ToList();
    }
}
