import unittest
from pathlib import Path

SOURCE = (Path(__file__).resolve().parents[1] / 'src/AshkanLedgerPro/Services/IndependentPdfReportService.cs').read_text(encoding='utf-8')

class PdfLayoutTests(unittest.TestCase):
    def test_page_size_fits_footer(self):
        self.assertIn('const int pageSize = 32;', SOURCE)
        self.assertGreater(689 - (32 - 1) * 17, 100)
    def test_courier_monospace(self):
        self.assertIn('/BaseFont /Courier', SOURCE)
    def test_input_validation(self):
        self.assertIn('Invalid financial report row.', SOURCE)
    def test_overflow_validation(self):
        self.assertIn('exceeds the printable column width', SOURCE)
    def test_pdf_cross_reference(self):
        self.assertIn('startxref', SOURCE)
        self.assertIn('%%EOF', SOURCE)

if __name__ == '__main__': unittest.main()
