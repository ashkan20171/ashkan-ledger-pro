import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / 'src/AshkanLedgerPro/Services/ManagementAnalyticsService.cs').read_text(encoding='utf-8')
MAIN = (ROOT / 'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')

class AnnualAnalyticsSourceTests(unittest.TestCase):
    def test_posted_only(self):
        self.assertIn('if (!entry.Posted || entry.Date.Year != year) continue;', SERVICE)
    def test_accounting_signs(self):
        self.assertIn('line.Credit - line.Debit', SERVICE)
        self.assertIn('line.Debit - line.Credit', SERVICE)
    def test_all_twelve_months(self):
        self.assertIn('Enumerable.Range(1, 12)', SERVICE)
    def test_invariant_csv(self):
        self.assertIn('CultureInfo.InvariantCulture', SERVICE)
    def test_ui_and_export(self):
        self.assertIn('AnnualRows()', MAIN)
        self.assertIn('ManagementAnalyticsService.ToCsv(AnnualRows())', MAIN)

if __name__ == '__main__':
    unittest.main()
