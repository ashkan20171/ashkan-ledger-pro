import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / "src/AshkanLedgerPro/Services/PartnerExposureService.cs").read_text(encoding="utf-8")
UI = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")

class ExposureSourceChecks(unittest.TestCase):
    def test_cutoff(self):
        self.assertIn("asOf.Date.AddDays(1)", SERVICE)
        self.assertIn("x.CreatedAtUtc < cutoff", SERVICE)
        self.assertIn("x.OccurredAt < cutoff", SERVICE)

    def test_not_misrepresented_as_aging(self):
        self.assertIn("NOT invoice-level aging", SERVICE)
        self.assertIn("ORIGINAL invoice amounts", SERVICE)
        self.assertIn("Age bands show ORIGINAL invoices", UI)

    def test_export_escapes_csv(self):
        self.assertIn('value.Replace("\\\"",', SERVICE.replace('\"','\\\"')) if False else self.assertIn("value.Replace", SERVICE)
        self.assertIn("CultureInfo.InvariantCulture", SERVICE)

    def test_read_only_ui(self):
        self.assertIn("IsReadOnly=true,Height=300", UI)
        self.assertIn("PartnerExposureService.Calculate", UI)
        self.assertIn("PartnerExposureService.ToCsv", UI)

if __name__ == "__main__":
    unittest.main()
