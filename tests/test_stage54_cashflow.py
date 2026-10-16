import pathlib, unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
class CashFlowSourceTests(unittest.TestCase):
 def test_months_and_net(self):
  s=(ROOT/'src/AshkanLedgerPro/Services/CashFlowAnalyticsService.cs').read_text(encoding='utf-8')
  self.assertIn('Enumerable.Range(1,12)',s)
  self.assertIn('Receipts - Payments',s)
  self.assertIn('OccurredAt.Year != year',s)
 def test_channels_and_invalid_amounts(self):
  s=(ROOT/'src/AshkanLedgerPro/Services/CashFlowAnalyticsService.cs').read_text(encoding='utf-8')
  for x in ('"Cash"','"Bank"','"Receipt"','"Payment"','t.Amount < 0'):
   self.assertIn(x,s)
 def test_dashboard_is_read_only(self):
  s=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')
  self.assertIn('cashDb.TreasuryTransactions.AsNoTracking()',s)
  self.assertIn('CashFlowAnalyticsService.ToCsv(CashRows())',s)
 def test_csv_invariant(self):
  s=(ROOT/'src/AshkanLedgerPro/Services/CashFlowAnalyticsService.cs').read_text(encoding='utf-8')
  self.assertIn('CultureInfo.InvariantCulture',s)
  self.assertIn('BankPayments',s)
if __name__=='__main__':unittest.main()
