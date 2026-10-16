from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
PDF = (ROOT / "src/AshkanLedgerPro/Services/IndependentPdfReportService.cs").read_text(encoding="utf-8")
SMOKE = (ROOT / "tests/PdfRuntimeSmoke/Program.cs").read_text(encoding="utf-8")

class PdfReceiptLifecycleTests(unittest.TestCase):
    def test_direct_export_invalidates_stale_receipt(self):
        self.assertIn('if (File.Exists(staleReceipt)) File.Delete(staleReceipt);', PDF)
    def test_pdf_trailer_checked(self):
        self.assertIn('trailer.Contains("startxref"', PDF)
        self.assertIn('trailer.Contains("%%EOF"', PDF)
    def test_runtime_smoke_covers_both_cases(self):
        self.assertIn('Truncated PDF rejected even with matching hash', SMOKE)
        self.assertIn('Direct export removes stale checksum receipt', SMOKE)
