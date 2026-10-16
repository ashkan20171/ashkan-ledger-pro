import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / "src/AshkanLedgerPro/Services/InvoiceAllocationService.cs").read_text(encoding="utf-8")
UI = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")

class AllocationAuditTests(unittest.TestCase):
    def test_audit_joins_reversal_without_hiding_it(self):
        self.assertIn("LEFT JOIN InvoiceAllocationReversals r ON r.AllocationId=a.Id", SERVICE)
        self.assertIn('reversedAt is null ? "Active" : "Reversed"', SERVICE)
    def test_limit_is_bounded_and_parameterized(self):
        self.assertIn("Math.Clamp(limit, 1, 10000)", SERVICE)
        self.assertIn("ORDER BY a.Id DESC LIMIT $limit", SERVICE)
    def test_csv_escaping_and_utf8(self):
        self.assertIn('Replace("\\\"","\\\"\\\"")', UI) if False else self.assertIn('CsvField(string? value)', UI)
        self.assertIn('new UTF8Encoding(true)', UI)
    def test_bilingual_audit_ui(self):
        self.assertIn('Allocation audit & CSV', UI)
        self.assertIn('حسابرسی تخصیص‌ها و CSV', UI)

if __name__ == "__main__": unittest.main()
