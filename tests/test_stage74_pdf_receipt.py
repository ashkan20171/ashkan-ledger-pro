import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
PDF = (ROOT / "src/AshkanLedgerPro/Services/IndependentPdfReportService.cs").read_text(encoding="utf-8")
UI = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")

class Stage74PdfReceiptTests(unittest.TestCase):
    def test_sha256_is_real_stream_hash(self):
        self.assertIn("SHA256.HashData(stream)", PDF)
        self.assertIn("Convert.ToHexString", PDF)
    def test_receipt_written_only_after_pdf(self):
        block = PDF.split("public static string SaveHistoricalWithChecksum", 1)[1].split("private static string Escape", 1)[0]
        self.assertLess(block.index("SaveHistorical(filePath"), block.index("SHA256.HashData"))
        self.assertLess(block.index("SHA256.HashData"), block.index("File.WriteAllText"))
    def test_atomic_receipt_and_cleanup(self):
        self.assertIn("File.Move(temporaryReceipt, receiptPath, overwrite: true)", PDF)
        self.assertIn("if (File.Exists(temporaryReceipt)) File.Delete(temporaryReceipt)", PDF)
    def test_receipt_is_sha256sum_compatible(self):
        self.assertIn('fullPath + ".sha256"', PDF)
        self.assertIn('Path.GetFileName(fullPath)', PDF)
        self.assertIn('encoderShouldEmitUTF8Identifier: false', PDF)
    def test_ui_uses_receipt_and_discloses_ascii_limit(self):
        self.assertIn("SaveHistoricalWithChecksum", UI)
        self.assertIn("checksumReceipt", UI)
        self.assertIn("English/ASCII PDF", UI)
    def test_original_validation_remains(self):
        self.assertIn("r.Allocated > r.Total", PDF)
        self.assertIn("checked(total + amount)", PDF)
        self.assertIn("File.Move(temporaryPath, fullPath, overwrite: true)", PDF)

if __name__ == "__main__": unittest.main()
