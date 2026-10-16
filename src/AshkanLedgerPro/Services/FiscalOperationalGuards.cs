using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

/// <summary>Defense-in-depth protection for operational records in locked fiscal periods.</summary>
public static class FiscalOperationalGuards
{
    public static void Install()
    {
        using var db = new LedgerDb();
        db.Database.EnsureCreated();
        // Install only after all operational tables have been initialized.
        // Existing triggers remain intact; installation is safe on repeated startups.
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_salesinvoices_insert BEFORE INSERT ON SalesInvoices WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.CreatedAtUtc,1,10) AND p.EndDate>=substr(NEW.CreatedAtUtc,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: SalesInvoices INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_salesinvoices_update BEFORE UPDATE ON SalesInvoices WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.CreatedAtUtc,1,10) AND p.EndDate>=substr(OLD.CreatedAtUtc,1,10)) OR EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.CreatedAtUtc,1,10) AND p.EndDate>=substr(NEW.CreatedAtUtc,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: SalesInvoices UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_salesinvoices_delete BEFORE DELETE ON SalesInvoices WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.CreatedAtUtc,1,10) AND p.EndDate>=substr(OLD.CreatedAtUtc,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: SalesInvoices DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_purchaseinvoices_insert BEFORE INSERT ON PurchaseInvoices WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.CreatedAtUtc,1,10) AND p.EndDate>=substr(NEW.CreatedAtUtc,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PurchaseInvoices INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_purchaseinvoices_update BEFORE UPDATE ON PurchaseInvoices WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.CreatedAtUtc,1,10) AND p.EndDate>=substr(OLD.CreatedAtUtc,1,10)) OR EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.CreatedAtUtc,1,10) AND p.EndDate>=substr(NEW.CreatedAtUtc,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PurchaseInvoices UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_purchaseinvoices_delete BEFORE DELETE ON PurchaseInvoices WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.CreatedAtUtc,1,10) AND p.EndDate>=substr(OLD.CreatedAtUtc,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PurchaseInvoices DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_treasurytransactions_insert BEFORE INSERT ON TreasuryTransactions WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.OccurredAt,1,10) AND p.EndDate>=substr(NEW.OccurredAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: TreasuryTransactions INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_treasurytransactions_update BEFORE UPDATE ON TreasuryTransactions WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.OccurredAt,1,10) AND p.EndDate>=substr(OLD.OccurredAt,1,10)) OR EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.OccurredAt,1,10) AND p.EndDate>=substr(NEW.OccurredAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: TreasuryTransactions UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_treasurytransactions_delete BEFORE DELETE ON TreasuryTransactions WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.OccurredAt,1,10) AND p.EndDate>=substr(OLD.OccurredAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: TreasuryTransactions DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_movements_insert BEFORE INSERT ON Movements WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.OccurredAt,1,10) AND p.EndDate>=substr(NEW.OccurredAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Movements INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_movements_update BEFORE UPDATE ON Movements WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.OccurredAt,1,10) AND p.EndDate>=substr(OLD.OccurredAt,1,10)) OR EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.OccurredAt,1,10) AND p.EndDate>=substr(NEW.OccurredAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Movements UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_movements_delete BEFORE DELETE ON Movements WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.OccurredAt,1,10) AND p.EndDate>=substr(OLD.OccurredAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Movements DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_paymentchecks_insert BEFORE INSERT ON PaymentChecks WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.CreatedAt,1,10) AND p.EndDate>=substr(NEW.CreatedAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PaymentChecks INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_paymentchecks_update BEFORE UPDATE ON PaymentChecks WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.CreatedAt,1,10) AND p.EndDate>=substr(OLD.CreatedAt,1,10)) OR EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(NEW.CreatedAt,1,10) AND p.EndDate>=substr(NEW.CreatedAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PaymentChecks UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_paymentchecks_delete BEFORE DELETE ON PaymentChecks WHEN EXISTS (SELECT 1 FROM FiscalPeriods p WHERE p.Locked=1 AND p.StartDate<=substr(OLD.CreatedAt,1,10) AND p.EndDate>=substr(OLD.CreatedAt,1,10)) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PaymentChecks DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_salesinvoicelines_insert BEFORE INSERT ON SalesInvoiceLines WHEN EXISTS (SELECT 1 FROM SalesInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=NEW.SalesInvoiceId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: SalesInvoiceLines INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_salesinvoicelines_update BEFORE UPDATE ON SalesInvoiceLines WHEN EXISTS (SELECT 1 FROM SalesInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=OLD.SalesInvoiceId) OR EXISTS (SELECT 1 FROM SalesInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=NEW.SalesInvoiceId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: SalesInvoiceLines UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_salesinvoicelines_delete BEFORE DELETE ON SalesInvoiceLines WHEN EXISTS (SELECT 1 FROM SalesInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=OLD.SalesInvoiceId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: SalesInvoiceLines DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_purchaseinvoicelines_insert BEFORE INSERT ON PurchaseInvoiceLines WHEN EXISTS (SELECT 1 FROM PurchaseInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=NEW.PurchaseInvoiceId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PurchaseInvoiceLines INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_purchaseinvoicelines_update BEFORE UPDATE ON PurchaseInvoiceLines WHEN EXISTS (SELECT 1 FROM PurchaseInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=OLD.PurchaseInvoiceId) OR EXISTS (SELECT 1 FROM PurchaseInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=NEW.PurchaseInvoiceId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PurchaseInvoiceLines UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_purchaseinvoicelines_delete BEFORE DELETE ON PurchaseInvoiceLines WHEN EXISTS (SELECT 1 FROM PurchaseInvoices h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAtUtc,1,10) AND p.EndDate>=substr(h.CreatedAtUtc,1,10) WHERE h.Id=OLD.PurchaseInvoiceId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: PurchaseInvoiceLines DELETE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_checkstatushistory_insert BEFORE INSERT ON CheckStatusHistory WHEN EXISTS (SELECT 1 FROM PaymentChecks h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAt,1,10) AND p.EndDate>=substr(h.CreatedAt,1,10) WHERE h.Id=NEW.CheckId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: CheckStatusHistory INSERT denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_checkstatushistory_update BEFORE UPDATE ON CheckStatusHistory WHEN EXISTS (SELECT 1 FROM PaymentChecks h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAt,1,10) AND p.EndDate>=substr(h.CreatedAt,1,10) WHERE h.Id=OLD.CheckId) OR EXISTS (SELECT 1 FROM PaymentChecks h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAt,1,10) AND p.EndDate>=substr(h.CreatedAt,1,10) WHERE h.Id=NEW.CheckId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: CheckStatusHistory UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_checkstatushistory_delete BEFORE DELETE ON CheckStatusHistory WHEN EXISTS (SELECT 1 FROM PaymentChecks h JOIN FiscalPeriods p ON p.Locked=1 AND p.StartDate<=substr(h.CreatedAt,1,10) AND p.EndDate>=substr(h.CreatedAt,1,10) WHERE h.Id=OLD.CheckId) BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: CheckStatusHistory DELETE denied'); END
            """);
        // Stock has no transaction date. Block edits/deletes to an item with any
        // movement in a locked period; this deliberately favors data integrity.
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_stock_update BEFORE UPDATE ON Stock
            WHEN EXISTS (SELECT 1 FROM Movements m JOIN FiscalPeriods p ON p.Locked=1
                AND p.StartDate<=substr(m.OccurredAt,1,10) AND p.EndDate>=substr(m.OccurredAt,1,10)
                WHERE m.StockItemId=OLD.Id)
            BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Stock UPDATE denied'); END
            """);
        db.Database.ExecuteSqlRaw("""
            CREATE TRIGGER IF NOT EXISTS guard_stock_delete BEFORE DELETE ON Stock
            WHEN EXISTS (SELECT 1 FROM Movements m JOIN FiscalPeriods p ON p.Locked=1
                AND p.StartDate<=substr(m.OccurredAt,1,10) AND p.EndDate>=substr(m.OccurredAt,1,10)
                WHERE m.StockItemId=OLD.Id)
            BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Stock DELETE denied'); END
            """);
    }
}
