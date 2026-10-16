import unittest
from pathlib import Path

SOURCE = (Path(__file__).resolve().parents[1] / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")

class ReportStage67Tests(unittest.TestCase):
    def test_responsive_chart(self):
        self.assertIn("trendCanvas.SizeChanged", SOURCE)
        self.assertIn("trendCanvas.ActualWidth", SOURCE)
        self.assertIn("ClipToBounds = true", SOURCE)
    def test_print_timestamp(self):
        self.assertIn("Generated (UTC)", SOURCE)
        self.assertIn("DateTime.UtcNow", SOURCE)
    def test_print_totals(self):
        self.assertIn("visible.Sum(x => x.RemainingAsOf)", SOURCE)
        self.assertIn("Invoice count", SOURCE)
    def test_print_uses_filtered_rows(self):
        self.assertIn("foreach (var x in visible) AddPrintRow", SOURCE)
        self.assertIn("print.PrintDocument", SOURCE)
