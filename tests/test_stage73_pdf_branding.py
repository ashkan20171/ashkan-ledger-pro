import pathlib
import re
import unittest

SOURCE = (pathlib.Path(__file__).resolve().parents[1] / 'src' / 'AshkanLedgerPro' / 'Services' / 'IndependentPdfReportService.cs').read_text(encoding='utf-8')

class Stage73PdfBrandingTests(unittest.TestCase):
    def test_vector_masthead(self):
        self.assertIn('0 765 595 77 re f', SOURCE)
        self.assertIn('ASHKAN LEDGER PRO', SOURCE)
    def test_table_header_background(self):
        self.assertIn('38 708 520 22 re f', SOURCE)
    def test_zebra_rows(self):
        self.assertIn('(i - start) % 2 == 1', SOURCE)
    def test_safe_page_capacity(self):
        self.assertIn('const int pageSize = 32;', SOURCE)
        self.assertIn('689 - (i - start) * 17', SOURCE)
        self.assertGreaterEqual(689 - 31 * 17, 119 + 18)
    def test_subtotal_and_footer(self):
        self.assertIn('Page subtotal:', SOURCE)
        self.assertIn('Sales due:', SOURCE)
        self.assertIn('Total remaining:', SOURCE)
    def test_atomic_save_and_validation_preserved(self):
        self.assertIn('File.Move(temporaryPath, fullPath, overwrite: true)', SOURCE)
        self.assertIn('r.Allocated > r.Total', SOURCE)
        self.assertIn('checked(total + amount)', SOURCE)

if __name__ == '__main__': unittest.main()
