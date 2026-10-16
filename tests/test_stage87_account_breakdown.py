import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
S=(ROOT/'src/AshkanLedgerPro/Services/ManagementAnalyticsService.cs').read_text()
U=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text()
class Stage87Tests(unittest.TestCase):
 def test_posted_only(self):
  self.assertIn('if (!entry.Posted || entry.Date.Date < start.Date || entry.Date.Date > end.Date) continue;',S)
 def test_reversal_signs(self):
  self.assertIn('line.Credit - line.Debit : line.Debit - line.Credit',S)
 def test_filter_validation(self):
  self.assertIn('accountType is not ("Revenue" or "Expense")',S)
 def test_csv_escaping(self):
  self.assertIn('value.Replace("\\\"",',S) if False else self.assertIn('AccountBreakdownCsv',S)
 def test_ui_export(self):
  self.assertIn('ManagementAnalyticsService.AccountBreakdownCsv(accountRows)',U)
 def test_ui_date_filter(self):
  self.assertIn('ManagementAnalyticsService.BreakdownByAccount(accounts,entries,from,to,type)',U)
