import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MAIN = (ROOT / "src/AshkanLedgerPro/MainWindow.xaml.cs").read_text(encoding="utf-8")
PAGINATOR = (ROOT / "src/AshkanLedgerPro/Services/ReportPageNumberPaginator.cs").read_text(encoding="utf-8")

class PrintLayoutTests(unittest.TestCase):
    def test_numbered_paginator_wired(self):
        self.assertIn("new Services.ReportPageNumberPaginator(paginator,", MAIN)
        self.assertIn("print.PrintDocument(numbered,", MAIN)
    def test_printable_area_used(self):
        self.assertIn("print.PrintableAreaWidth, print.PrintableAreaHeight", MAIN)
    def test_header_and_striped_rows(self):
        self.assertIn("AddPrintRow(bool header", MAIN)
        self.assertIn("Brushes.AliceBlue", MAIN)
    def test_footer_both_languages(self):
        self.assertIn("صفحه {pageNumber + 1}", PAGINATOR)
        self.assertIn("Page {pageNumber + 1}", PAGINATOR)
    def test_preserves_existing_pdf_export(self):
        self.assertIn("IndependentPdfReportService.SaveHistoricalWithChecksum(", MAIN)
