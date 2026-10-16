import sqlite3
import unittest

class ReversalTests(unittest.TestCase):
    def setUp(self):
        self.db = sqlite3.connect(":memory:")
        self.db.executescript("""
        CREATE TABLE InvoiceAllocations(Id INTEGER PRIMARY KEY, TreasuryTransactionId INTEGER, InvoiceKind TEXT, InvoiceId INTEGER, Amount TEXT, AllocatedAtUtc TEXT);
        CREATE TABLE InvoiceAllocationReversals(Id INTEGER PRIMARY KEY AUTOINCREMENT, AllocationId INTEGER NOT NULL UNIQUE REFERENCES InvoiceAllocations(Id), Reason TEXT NOT NULL CHECK(length(trim(Reason)) >= 8), ReversedBy TEXT NOT NULL, ReversedAtUtc TEXT NOT NULL);
        CREATE TRIGGER trg_allocation_reversals_no_update BEFORE UPDATE ON InvoiceAllocationReversals BEGIN SELECT RAISE(ABORT,'Reversal audit is immutable'); END;
        CREATE TRIGGER trg_allocation_reversals_no_delete BEFORE DELETE ON InvoiceAllocationReversals BEGIN SELECT RAISE(ABORT,'Reversal audit is immutable'); END;
        INSERT INTO InvoiceAllocations VALUES(1,7,'Sales',12,'125.00','2026-10-16');
        """)
    def tearDown(self): self.db.close()
    def reverse(self, reason="Duplicate allocation"):
        return self.db.execute("INSERT INTO InvoiceAllocationReversals(AllocationId,Reason,ReversedBy,ReversedAtUtc) SELECT Id,?,?,? FROM InvoiceAllocations WHERE Id=? AND NOT EXISTS(SELECT 1 FROM InvoiceAllocationReversals WHERE AllocationId=?)",(reason,"admin","2026-10-16",1,1)).rowcount
    def test_reversal_is_recorded(self):
        self.assertEqual(self.reverse(),1)
        self.assertEqual(self.db.execute("SELECT ReversedBy FROM InvoiceAllocationReversals").fetchone()[0],"admin")
    def test_reversal_is_one_time(self):
        self.reverse(); self.assertEqual(self.reverse(),0)
    def test_reversal_releases_allocation(self):
        self.reverse()
        rows=self.db.execute("SELECT Id FROM InvoiceAllocations a WHERE NOT EXISTS(SELECT 1 FROM InvoiceAllocationReversals r WHERE r.AllocationId=a.Id)").fetchall()
        self.assertEqual(rows,[])
    def test_audit_cannot_be_updated(self):
        self.reverse()
        with self.assertRaises(sqlite3.IntegrityError): self.db.execute("UPDATE InvoiceAllocationReversals SET Reason='Changed reason' WHERE AllocationId=1")
    def test_audit_cannot_be_deleted(self):
        self.reverse()
        with self.assertRaises(sqlite3.IntegrityError): self.db.execute("DELETE FROM InvoiceAllocationReversals WHERE AllocationId=1")
    def test_short_reason_rejected(self):
        with self.assertRaises(sqlite3.IntegrityError): self.reverse("bad")

if __name__=='__main__': unittest.main()
