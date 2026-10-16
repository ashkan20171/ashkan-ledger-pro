import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "src" / "AshkanLedgerPro"

class Stage70CompileRegressions(unittest.TestCase):
    def test_undefined_factory_removed(self):
        main = (ROOT / "MainWindow.xaml.cs").read_text(encoding="utf-8")
        self.assertNotIn("NewDb()", main)
        self.assertGreaterEqual(main.count("new AshkanLedgerPro.Data.LedgerDb()"), 4)

    def test_pdf_file_io_namespace(self):
        pdf = (ROOT / "Services" / "IndependentPdfReportService.cs").read_text(encoding="utf-8")
        self.assertIn("using System.IO;", pdf)
        for name in ("FileStream", "FileMode", "FileAccess", "FileShare"):
            self.assertIn(name, pdf)

if __name__ == "__main__":
    unittest.main()
