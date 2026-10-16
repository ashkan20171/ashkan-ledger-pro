import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT / 'src/AshkanLedgerPro/Services/YearOverYearService.cs').read_text(encoding='utf-8')
UI = (ROOT / 'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')

class YearOverYearSourceTests(unittest.TestCase):
    def test_reuses_posted_only_analytics(self):
        self.assertIn('ManagementAnalyticsService.ForYear(accounts, entries, currentYear - 1)', SERVICE)
        self.assertIn('ManagementAnalyticsService.ForYear(accounts, entries, currentYear)', SERVICE)
        self.assertIn('if (!entry.Posted', (ROOT / 'src/AshkanLedgerPro/Services/ManagementAnalyticsService.cs').read_text(encoding='utf-8'))
    def test_zero_baseline_is_undefined(self):
        self.assertIn('PreviousProfit == 0 ? null', SERVICE)
        self.assertIn('Math.Abs(PreviousProfit)', SERVICE)
    def test_export_invariant_and_no_missing_as_zero(self):
        self.assertIn('CultureInfo.InvariantCulture', SERVICE)
        self.assertIn('?? "N/A"', SERVICE)
        self.assertIn('new UTF8Encoding(true)', UI)
    def test_ui_is_read_only_and_bilingual(self):
        self.assertIn('Year-over-year profitability', UI)
        self.assertIn('مقایسه سودآوری دو سال متوالی', UI)
        self.assertIn('yoyGrid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true', UI)
    def test_no_write_operations_in_report_service(self):
        for forbidden in ('SaveChanges', 'ExecuteSqlRaw', 'File.WriteAllText', 'INSERT INTO', 'DELETE FROM'):
            self.assertNotIn(forbidden, SERVICE)

if __name__ == '__main__': unittest.main()
