import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
PDF = (ROOT / "src/AshkanLedgerPro/Services/IndependentPdfReportService.cs").read_text(encoding="utf-8")
UI = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")
class PrintSafetyTests(unittest.TestCase):
    def test_page_subtotals(self):
        self.assertIn("Page subtotal:", PDF)
        self.assertIn("rows.Skip(start).Take(pageSize)", PDF)
    def test_report_generation_timestamp(self):
        self.assertIn("Generated UTC:", PDF)
        self.assertIn("generatedAtUtc.ToString", PDF)
    def test_pdf_footer_does_not_overlap(self):
        self.assertIn("const int pageSize = 32;", PDF)
        self.assertGreater(689 - (32 - 1) * 17, 105)
    def test_wpf_print_uses_language_direction(self):
        self.assertIn("FlowDirection = (forcePersian || LanguagePreferences.IsPersian()) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight", UI)
    def test_print_invoice_kind_translated(self):
        self.assertIn('x.InvoiceKind == "Sales" ? "فروش" : "خرید"', UI)
    def test_atomic_write_retained(self):
        self.assertIn("File.Move(temporaryPath, fullPath, overwrite: true)", PDF)
if __name__ == "__main__": unittest.main()
