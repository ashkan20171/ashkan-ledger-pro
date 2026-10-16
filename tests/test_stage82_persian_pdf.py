import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")

class PersianPdfTests(unittest.TestCase):
    def test_button_and_event(self):
        self.assertIn('var persianPdf = new Button', SOURCE)
        self.assertIn('persianPdf.Click += (_,_) => PrintHistoricalReport(true);', SOURCE)
    def test_forced_rtl(self):
        self.assertIn('(forcePersian || LanguagePreferences.IsPersian()) ? FlowDirection.RightToLeft', SOURCE)
    def test_print_driver_and_not_fake_pdf(self):
        self.assertIn('var print = new PrintDialog();', SOURCE)
        self.assertIn('Microsoft Print to PDF', SOURCE)
    def test_existing_checksum_export_preserved(self):
        self.assertIn('IndependentPdfReportService.SaveHistoricalWithChecksum(', SOURCE)

if __name__ == "__main__": unittest.main()
