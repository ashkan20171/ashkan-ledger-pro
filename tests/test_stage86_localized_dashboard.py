import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
UI=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text()
SERVICE=(ROOT/'src/AshkanLedgerPro/Services/ManagementAnalyticsService.cs').read_text()
class Stage86Tests(unittest.TestCase):
 def test_localized_input(self):
  self.assertIn('LocalizedDateInput.Format(DateTime.Today.AddDays(-29),persian)',UI)
  self.assertIn('LocalizedDateInput.TryParse(rangeFrom.Text,persian,out var from)',UI)
 def test_comparison_chart(self):
  self.assertIn('CompareBar(T("Revenue","درآمد")',UI)
  self.assertIn('CompareBar(T("Net profit","سود خالص")',UI)
 def test_growth_zero_baseline(self):
  self.assertIn('previous == 0m ? null',SERVICE)
  self.assertIn('Math.Abs(previous)',SERVICE)
 def test_growth_in_dashboard(self):
  self.assertIn('ManagementAnalyticsService.GrowthLabel(selectedPeriod.Revenue,previousPeriod.Revenue)',UI)
