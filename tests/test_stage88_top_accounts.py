import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")

class Stage88Tests(unittest.TestCase):
    def test_top_five_and_all_accounts_denominator(self):
        self.assertIn("accountRows.Take(5).ToList()", SRC)
        self.assertIn("accountRows.Sum(x=>Math.Abs(x.Amount))", SRC)
    def test_negative_and_zero_handling(self):
        self.assertIn("item.Amount<0m", SRC)
        self.assertIn("absoluteTotal==0m", SRC)
    def test_bilingual_and_refresh(self):
        self.assertIn('T("Top five accounts by contribution","پنج حساب برتر بر اساس سهم")', SRC)
        self.assertIn("accountTopChart.Children.Clear()", SRC)
        self.assertIn("rangeRefresh.Click+=(_,_)=>{try{RefreshAccounts();}", SRC)
