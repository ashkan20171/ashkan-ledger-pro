import pathlib
import unittest

ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=(ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')

class Stage92BudgetFixTests(unittest.TestCase):
 def test_panel_initialized_once(self):
  self.assertEqual(SOURCE.count('var budgetCards=new WrapPanel'),1)
 def test_panel_initialized_before_callbacks(self):
  self.assertLess(SOURCE.index('var budgetCards=new WrapPanel'),SOURCE.index('budgetSave.Click+='))
  self.assertLess(SOURCE.index('var budgetCards=new WrapPanel'),SOURCE.index('budgetLoad.Click+='))
 def test_preset_grid_and_refresh(self):
  self.assertIn('var savedBudgets=new DataGrid',SOURCE)
  self.assertIn('void RefreshSavedBudgets()',SOURCE)
  self.assertIn('budgetListRefresh.Click+=',SOURCE)
 def test_refresh_after_mutations(self):
  self.assertIn('budgetStore.Save(new BudgetPresetStore.Preset(key.Account.Id,key.From,key.To,amount));\n   RefreshSavedBudgets();',SOURCE)
  self.assertIn('bool deleted=budgetStore.Delete(key.Account.Id,key.From,key.To);\n   RefreshSavedBudgets();',SOURCE)
