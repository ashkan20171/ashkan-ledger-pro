import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / "src/AshkanLedgerPro/Services/IndependentPdfReportService.cs").read_text()
UI = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text()
class PdfVerifyTests(unittest.TestCase):
    def test_verification_method(self): self.assertIn("public static bool VerifyChecksum(string pdfPath)", SERVICE)
    def test_checks_receipt_name(self): self.assertIn("Path.GetFileName(fullPath)", SERVICE)
    def test_computes_sha256(self): self.assertIn("SHA256.HashData(stream)", SERVICE)
    def test_rejects_malformed_receipt(self): self.assertIn("Invalid or mismatched SHA-256 receipt.", SERVICE)
    def test_verification_ui(self): self.assertIn("verifyPdf.Click +=", UI)
    def test_verification_is_read_only(self):
        body=SERVICE.split("public static bool VerifyChecksum(string pdfPath)",1)[1].split("private static string Escape",1)[0]
        self.assertNotIn("File.Write", body)
        self.assertNotIn("File.Move", body)
if __name__ == '__main__': unittest.main()
