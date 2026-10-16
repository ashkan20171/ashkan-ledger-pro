import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
PDF = (ROOT / "src/AshkanLedgerPro/Services/IndependentPdfReportService.cs").read_text(encoding="utf-8")

class Stage71PdfRegression(unittest.TestCase):
    def test_atomic_write(self):
        self.assertIn("File.Move(temporaryPath, fullPath, overwrite: true)", PDF)
        self.assertIn("File.Exists(temporaryPath)", PDF)
    def test_arithmetic(self):
        self.assertIn("r.Remaining != r.Total - r.Allocated", PDF)
        self.assertIn("r.Allocated > r.Total", PDF)
    def test_kind_validation(self):
        self.assertIn('r.Kind != "Sales" && r.Kind != "Purchase"', PDF)
    def test_checked_summaries(self):
        self.assertIn("checked(total + amount)", PDF)
    def test_split_totals(self):
        self.assertIn('"Sales due: "', PDF)
        self.assertIn('Purchase due: ', PDF)

if __name__ == "__main__": unittest.main()
