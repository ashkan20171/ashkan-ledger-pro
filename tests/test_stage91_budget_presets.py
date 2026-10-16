from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[1]
SERVICE = (ROOT/'src/AshkanLedgerPro/Services/BudgetPresetStore.cs').read_text()
UI = (ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text()
class Stage91Tests(unittest.TestCase):
 def test_atomic_save(self):
  self.assertIn('File.Move(temp, path, overwrite: true)', SERVICE)
  self.assertIn('finally { if (File.Exists(temp)) File.Delete(temp); }', SERVICE)
 def test_validation(self):
  self.assertIn('row.Limit < 0m', SERVICE)
  self.assertIn('row.From > row.To', SERVICE)
  self.assertIn('Duplicate budget preset keys.', SERVICE)
 def test_ui_actions(self):
  for phrase in ('budgetSave.Click', 'budgetLoad.Click', 'budgetDelete.Click', 'BudgetKey()'):
   self.assertIn(phrase, UI)
 def test_not_in_ledger(self):
  self.assertIn('budget-presets.json', SERVICE)
  self.assertNotIn('ledger.db', SERVICE)
