import sqlite3
import unittest

class Stage49Tests(unittest.TestCase):
    def setUp(self):
        self.db=sqlite3.connect(":memory:")
        self.db.executescript("""
        CREATE TABLE FiscalPeriods (StartDate TEXT,EndDate TEXT,Locked INTEGER);
        CREATE TABLE Stock (Id INTEGER PRIMARY KEY, Quantity INTEGER);
        CREATE TABLE Movements (StockItemId INTEGER,OccurredAt TEXT);
        CREATE TABLE BankReconciliationRuns (Id INTEGER PRIMARY KEY);
        CREATE TABLE BankReconciliationRows (Id INTEGER PRIMARY KEY,RunId INTEGER);
        CREATE TRIGGER guard_stock_update BEFORE UPDATE ON Stock
        WHEN EXISTS (SELECT 1 FROM Movements m JOIN FiscalPeriods p ON p.Locked=1
          AND p.StartDate<=substr(m.OccurredAt,1,10) AND p.EndDate>=substr(m.OccurredAt,1,10)
          WHERE m.StockItemId=OLD.Id)
        BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Stock UPDATE denied'); END;
        CREATE TRIGGER guard_stock_delete BEFORE DELETE ON Stock
        WHEN EXISTS (SELECT 1 FROM Movements m JOIN FiscalPeriods p ON p.Locked=1
          AND p.StartDate<=substr(m.OccurredAt,1,10) AND p.EndDate>=substr(m.OccurredAt,1,10)
          WHERE m.StockItemId=OLD.Id)
        BEGIN SELECT RAISE(ABORT, 'Locked fiscal period: Stock DELETE denied'); END;
        CREATE TRIGGER guard_runs_update BEFORE UPDATE ON BankReconciliationRuns
        BEGIN SELECT RAISE(ABORT,'Immutable reconciliation history'); END;
        CREATE TRIGGER guard_rows_delete BEFORE DELETE ON BankReconciliationRows
        BEGIN SELECT RAISE(ABORT,'Immutable reconciliation history'); END;
        INSERT INTO Stock VALUES (1,10),(2,20);
        INSERT INTO Movements VALUES (1,'2026-01-10');
        INSERT INTO FiscalPeriods VALUES ('2026-01-01','2026-01-31',1);
        INSERT INTO BankReconciliationRuns VALUES (1);
        INSERT INTO BankReconciliationRows VALUES (1,1);
        """)
    def tearDown(self): self.db.close()
    def test_locked_stock_update(self):
        with self.assertRaises(sqlite3.IntegrityError): self.db.execute('UPDATE Stock SET Quantity=11 WHERE Id=1')
    def test_locked_stock_delete(self):
        with self.assertRaises(sqlite3.IntegrityError): self.db.execute('DELETE FROM Stock WHERE Id=1')
    def test_unrelated_stock_allowed(self):
        self.db.execute('UPDATE Stock SET Quantity=21 WHERE Id=2')
        self.assertEqual(self.db.execute('SELECT Quantity FROM Stock WHERE Id=2').fetchone()[0],21)
    def test_unlock_allows_stock_edit(self):
        self.db.execute('UPDATE FiscalPeriods SET Locked=0')
        self.db.execute('UPDATE Stock SET Quantity=11 WHERE Id=1')
    def test_reconciliation_update_denied(self):
        with self.assertRaises(sqlite3.IntegrityError): self.db.execute('UPDATE BankReconciliationRuns SET Id=2 WHERE Id=1')
    def test_reconciliation_delete_denied(self):
        with self.assertRaises(sqlite3.IntegrityError): self.db.execute('DELETE FROM BankReconciliationRows WHERE Id=1')

if __name__=='__main__': unittest.main()
