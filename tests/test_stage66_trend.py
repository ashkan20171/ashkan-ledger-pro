import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = (ROOT / 'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')

class TrendSourceTests(unittest.TestCase):
    def test_trend_uses_filtered_snapshot(self):
        self.assertIn('DrawTrend(visible);', SOURCE)
    def test_trend_separates_invoice_types(self):
        self.assertIn('new[] { "Sales", "Purchase" }', SOURCE)
        self.assertIn('x.InvoiceKind == type', SOURCE)
    def test_trend_uses_issue_month(self):
        self.assertIn('x.InvoiceDateUtc.Month == month.Month', SOURCE)
    def test_trend_handles_empty_results(self):
        self.assertIn('if (months.Count == 0)', SOURCE)
    def test_pdf_is_printer_based(self):
        self.assertIn('Microsoft Print to PDF', SOURCE)
        self.assertIn('PrintDialog()', SOURCE)
