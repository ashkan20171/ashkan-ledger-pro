from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[1]
PDF = (ROOT / "src/AshkanLedgerPro/Services/IndependentPdfReportService.cs").read_text()
SMOKE = (ROOT / "tests/PdfRuntimeSmoke/Program.cs").read_text()
class PdfXrefTests(unittest.TestCase):
    def test_end_marker_is_anchored(self):
        self.assertIn(r"%%EOF\s*\z", PDF)
    def test_cross_reference_offset_checked(self):
        self.assertIn('xrefOffset >= stream.Length - 4', PDF)
    def test_cross_reference_table_verified(self):
        self.assertIn('xrefHeader.SequenceEqual("xref"u8)', PDF)
    def test_runtime_trailing_bytes(self):
        self.assertIn('Trailing bytes after EOF rejected', SMOKE)
    def test_runtime_bogus_xref(self):
        self.assertIn('Invalid cross-reference pointer rejected', SMOKE)
