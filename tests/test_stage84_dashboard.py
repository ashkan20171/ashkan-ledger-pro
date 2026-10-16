import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')
class DashboardStage84Tests(unittest.TestCase):
 def test_refresh_action(self):
  self.assertIn('refreshDashboard.Click+=(_,_)=>ShowOverview()',SOURCE)
 def test_zero_revenue_margin(self):
  self.assertIn('revenue==0m?(decimal?)null',SOURCE)
  self.assertIn('sixMonthRevenue==0m?(decimal?)null',SOURCE)
 def test_six_month_summary(self):
  for token in ('sixMonthRevenue=monthly.Sum','sixMonthExpense=monthly.Sum','sixMonthNet=sixMonthRevenue-sixMonthExpense','p.Children.Add(trendCards)'):
   self.assertIn(token,SOURCE)
 def test_localization(self):
  self.assertIn('حاشیه سود شش‌ماهه',SOURCE)
  self.assertIn('به‌روزرسانی داشبورد',SOURCE)
