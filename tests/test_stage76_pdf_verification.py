import unittest
from pathlib import Path

SOURCE = (Path(__file__).resolve().parents[1] / 'src/AshkanLedgerPro/Services/IndependentPdfReportService.cs').read_text()

class Stage76PdfVerificationTests(unittest.TestCase):
    def test_receipt_size_limit(self):
        self.assertIn('Length > 4096', SOURCE)
    def test_strict_utf8(self):
        self.assertIn('new UTF8Encoding(false, true)', SOURCE)
    def test_pdf_magic(self):
        self.assertIn('header.SequenceEqual("%PDF-"u8)', SOURCE)
    def test_hash_comparison(self):
        self.assertIn('CryptographicOperations.FixedTimeEquals(actual, expected)', SOURCE)
    def test_rewind_before_hashing(self):
        self.assertIn('stream.Position = 0;', SOURCE)

if __name__ == '__main__': unittest.main()
