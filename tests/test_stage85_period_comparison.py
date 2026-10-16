import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
SERVICE=(ROOT/'src/AshkanLedgerPro/Services/ManagementAnalyticsService.cs').read_text()
UI=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text()
class Stage85Tests(unittest.TestCase):
 def test_inclusive_dates_and_posted_only(self):
  self.assertIn('entry.Date.Date < start.Date || entry.Date.Date > end.Date',SERVICE)
  self.assertIn('!entry.Posted',SERVICE)
 def test_previous_equal_period(self):
  self.assertIn('var days=(to-from).Days+1',UI)
  self.assertIn('from.AddDays(-days),from.AddDays(-1)',UI)
 def test_export_and_margin(self):
  self.assertIn('ComparePeriodsCsv',SERVICE)
  self.assertIn('Revenue == 0m ? null',SERVICE)
  self.assertIn('new UTF8Encoding(true)',UI)
 def test_bilingual_controls(self):
  self.assertIn('مقایسه بازه‌های دلخواه',UI)
  self.assertIn('خروجی CSV مقایسه',UI)
