"""Verify the actual shipped C# SQLite guards, not copies of hand-written SQL."""
from pathlib import Path
import re
import sqlite3
import unittest

ROOT = Path(__file__).resolve().parents[1]
SERVICES = ROOT / "src" / "AshkanLedgerPro" / "Services"
SOURCE = (SERVICES / "FiscalOperationalGuards.cs").read_text(encoding="utf-8")


def stock_triggers():
    result = re.findall(r"CREATE TRIGGER IF NOT EXISTS guard_stock_(?:update|delete) .*? END", SOURCE, re.S)
    if len(result) != 2:
        raise AssertionError(f"Expected two shipped stock triggers; found {len(result)}")
    return result


class SourceStockGuardTests(unittest.TestCase):
    def setUp(self):
        self.db = sqlite3.connect(":memory:")
        self.db.executescript("""
            CREATE TABLE FiscalPeriods (StartDate TEXT, EndDate TEXT, Locked INTEGER);
            CREATE TABLE Stock (Id INTEGER PRIMARY KEY, Quantity INTEGER);
            CREATE TABLE Movements (StockItemId INTEGER, OccurredAt TEXT);
        """)
        for statement in stock_triggers():
            self.db.execute(statement)
        self.db.executescript("""
            INSERT INTO Stock VALUES (1, 10), (2, 20);
            INSERT INTO Movements VALUES (1, '2026-06-15');
            INSERT INTO FiscalPeriods VALUES ('2026-01-01', '2026-12-31', 1);
        """)

    def tearDown(self):
        self.db.close()

    def test_locked_stock_cannot_be_updated(self):
        with self.assertRaises(sqlite3.IntegrityError):
            self.db.execute("UPDATE Stock SET Quantity=12 WHERE Id=1")
        self.assertEqual(self.db.execute("SELECT Quantity FROM Stock WHERE Id=1").fetchone()[0], 10)

    def test_locked_stock_cannot_be_deleted(self):
        with self.assertRaises(sqlite3.IntegrityError):
            self.db.execute("DELETE FROM Stock WHERE Id=1")

    def test_unrelated_stock_is_editable(self):
        self.db.execute("UPDATE Stock SET Quantity=25 WHERE Id=2")
        self.assertEqual(self.db.execute("SELECT Quantity FROM Stock WHERE Id=2").fetchone()[0], 25)

    def test_unlock_allows_changes(self):
        self.db.execute("UPDATE FiscalPeriods SET Locked=0")
        self.db.execute("UPDATE Stock SET Quantity=12 WHERE Id=1")
        self.assertEqual(self.db.execute("SELECT Quantity FROM Stock WHERE Id=1").fetchone()[0], 12)

    def test_stock_triggers_are_installed(self):
        names = {x[0] for x in self.db.execute("SELECT name FROM sqlite_master WHERE type='trigger'")}
        self.assertEqual(names, {"guard_stock_update", "guard_stock_delete"})


class ReconciliationGuardSourceTests(unittest.TestCase):
    def test_reconciliation_guards_are_generated_for_both_tables(self):
        source = (SERVICES / "ReconciliationHistoryService.cs").read_text(encoding="utf-8")
        self.assertIn("Immutable reconciliation history", source)
        self.assertIn("CREATE TRIGGER IF NOT EXISTS", source)
        self.assertIn("BankReconciliationRuns", source)
        self.assertIn("BankReconciliationRows", source)


if __name__ == "__main__":
    unittest.main(verbosity=2)
