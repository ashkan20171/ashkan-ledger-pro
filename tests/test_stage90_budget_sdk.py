import json
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[1]
class Stage90Tests(unittest.TestCase):
 def test_sdk_allows_major_rollforward(self):
  sdk=json.loads((ROOT/'global.json').read_text())['sdk']
  self.assertEqual(sdk['version'],'8.0.100')
  self.assertEqual(sdk['rollForward'],'latestMajor')
 def test_budget_service_validates_inputs(self):
  source=(ROOT/'src/AshkanLedgerPro/Services/BudgetAnalyticsService.cs').read_text()
  for token in ('account.Type != "Expense"','limit < 0m','!entry.Posted','line.Debit - line.Credit','Actual > Limit','CultureInfo.InvariantCulture'):
   self.assertIn(token,source)
 def test_budget_ui_present(self):
  source=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text()
  for token in ('BudgetAnalyticsService.Evaluate','BudgetAnalyticsService.ToCsv','budgetCheck.Click','budgetExport.Click'):
   self.assertIn(token,source)
 def test_budget_is_session_only(self):
  self.assertIn('session-only',(ROOT/'README_STAGE90.md').read_text())
