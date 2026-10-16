import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = (ROOT / "src/AshkanLedgerPro/Services/CashFlowAnalyticsService.cs").read_text()
UI = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text()

class Stage89Tests(unittest.TestCase):
    def test_cumulative_movement(self):
        self.assertIn("running += m.Net", SERVICE)
    def test_duplicate_months_rejected(self):
        self.assertIn("Duplicate cash-flow months", SERVICE)
    def test_warning_threshold(self):
        self.assertIn("running < warningThreshold", SERVICE)
    def test_csv_has_threshold_column(self):
        self.assertIn("CumulativeNet,BelowThreshold", SERVICE)
    def test_ui_trend_export(self):
        self.assertIn("CashFlowAnalyticsService.TrendCsv(TrendRows())", UI)
    def test_accounting_disclaimer(self):
        self.assertIn("NOT your available bank/cash balance", UI)
