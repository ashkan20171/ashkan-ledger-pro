"""SQLite trigger regression checks against the shipped C# trigger SQL. Run: python tests/test_fiscal_guards.py"""
from pathlib import Path
import re
import sqlite3
import unittest

ROOT = Path(__file__).resolve().parents[1]
SERVICES = ROOT / 'src' / 'AshkanLedgerPro' / 'Services'
FILES = ['FiscalPeriodService.cs', 'FiscalOperationalGuards.cs']
TRIGGERS = []
for name in FILES:
    text = (SERVICES / name).read_text(encoding='utf-8')
    TRIGGERS += re.findall(r'CREATE TRIGGER IF NOT EXISTS .*? END', text, re.S)

TABLES = {
    'Entries': 'Id INTEGER PRIMARY KEY, Date TEXT',
    'Lines': 'Id INTEGER PRIMARY KEY, JournalEntryId INTEGER',
    'SalesInvoices': 'Id INTEGER PRIMARY KEY, CreatedAtUtc TEXT',
    'SalesInvoiceLines': 'Id INTEGER PRIMARY KEY, SalesInvoiceId INTEGER',
    'PurchaseInvoices': 'Id INTEGER PRIMARY KEY, CreatedAtUtc TEXT',
    'PurchaseInvoiceLines': 'Id INTEGER PRIMARY KEY, PurchaseInvoiceId INTEGER',
    'TreasuryTransactions': 'Id INTEGER PRIMARY KEY, OccurredAt TEXT',
    'Movements': 'Id INTEGER PRIMARY KEY, OccurredAt TEXT',
    'PaymentChecks': 'Id INTEGER PRIMARY KEY, CreatedAt TEXT',
    'CheckStatusHistory': 'Id INTEGER PRIMARY KEY, CheckId INTEGER',
    'Stock': 'Id INTEGER PRIMARY KEY, Quantity INTEGER',
    'BankReconciliationRuns': 'Id INTEGER PRIMARY KEY',
    'BankReconciliationRows': 'Id INTEGER PRIMARY KEY, RunId INTEGER',
}
DATE_COL = {'Entries': 'Date', 'SalesInvoices': 'CreatedAtUtc', 'PurchaseInvoices': 'CreatedAtUtc',
            'TreasuryTransactions': 'OccurredAt', 'Movements': 'OccurredAt', 'PaymentChecks': 'CreatedAt'}
CHILDREN = {'Lines': ('JournalEntryId', 'Entries'), 'SalesInvoiceLines': ('SalesInvoiceId', 'SalesInvoices'),
            'PurchaseInvoiceLines': ('PurchaseInvoiceId', 'PurchaseInvoices'),
            'CheckStatusHistory': ('CheckId', 'PaymentChecks')}

class FiscalGuardTests(unittest.TestCase):
    def setUp(self):
        self.db = sqlite3.connect(':memory:')
        self.db.execute('CREATE TABLE FiscalPeriods (Id INTEGER PRIMARY KEY, StartDate TEXT, EndDate TEXT, Locked INTEGER)')
        for table, cols in TABLES.items():
            self.db.execute(f'CREATE TABLE {table} ({cols})')
        for sql in TRIGGERS:
            self.db.execute(sql)
        self.assertEqual(len(TRIGGERS), 32)
        self.db.execute("INSERT INTO FiscalPeriods VALUES (1,'2026-01-01','2026-12-31',1)")

    def tearDown(self): self.db.close()

    def test_locked_parent_insert_update_delete_rejected(self):
        for table, col in DATE_COL.items():
            with self.subTest(table=table):
                with self.assertRaises(sqlite3.IntegrityError):
                    self.db.execute(f"INSERT INTO {table}(Id,{col}) VALUES (1,'2026-06-15')")
                self.db.execute(f"INSERT INTO {table}(Id,{col}) VALUES (1,'2027-06-15')")
                with self.assertRaises(sqlite3.IntegrityError):
                    self.db.execute(f"UPDATE {table} SET {col}='2026-06-15' WHERE Id=1")
                self.db.execute(f"UPDATE {table} SET {col}='2027-06-16' WHERE Id=1")
                self.db.execute(f"DELETE FROM {table} WHERE Id=1")

    def test_locked_parent_child_writes_rejected(self):
        for child, (fk, parent) in CHILDREN.items():
            with self.subTest(child=child):
                col = DATE_COL[parent]
                self.db.execute(f"INSERT INTO {parent}(Id,{col}) VALUES (5,'2027-01-01')")
                self.db.execute(f"INSERT INTO {child}(Id,{fk}) VALUES (7,5)")
                # Lock the previously open parent period after the child exists.
                self.db.execute("UPDATE FiscalPeriods SET Locked=0 WHERE Id=1")
                self.db.execute(f"UPDATE {parent} SET {col}='2026-07-01' WHERE Id=5")
                self.db.execute("UPDATE FiscalPeriods SET Locked=1 WHERE Id=1")
                with self.assertRaises(sqlite3.IntegrityError):
                    self.db.execute(f"INSERT INTO {child}(Id,{fk}) VALUES (8,5)")
                with self.assertRaises(sqlite3.IntegrityError):
                    self.db.execute(f"UPDATE {child} SET {fk}=5 WHERE Id=7")
                with self.assertRaises(sqlite3.IntegrityError):
                    self.db.execute(f"DELETE FROM {child} WHERE Id=7")
                self.db.execute("UPDATE FiscalPeriods SET Locked=0 WHERE Id=1")
                self.db.execute(f"DELETE FROM {child} WHERE Id=7")
                self.db.execute(f"DELETE FROM {parent} WHERE Id=5")
                self.db.execute("UPDATE FiscalPeriods SET Locked=1 WHERE Id=1")

if __name__ == '__main__': unittest.main(verbosity=2)
