import pathlib, unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
SERVICE = (ROOT/'src/AshkanLedgerPro/Services/InvoiceAllocationService.cs').read_text(encoding='utf-8')
UI = (ROOT/'src/AshkanLedgerPro/MainWindow.xaml.cs').read_text(encoding='utf-8')
class HistoricalDashboardTests(unittest.TestCase):
    def test_cutoff_excludes_future_allocation(self):
        self.assertIn('if (allocatedAt > cutoff) continue;', SERVICE)
    def test_reversal_after_cutoff_still_counts(self):
        self.assertIn('if (reversedAt <= cutoff) continue;', SERVICE)
    def test_invoices_created_after_cutoff_excluded(self):
        self.assertIn('if (issueUtc > cutoff) return;', SERVICE)
    def test_kpis_derived_from_filtered_rows(self):
        self.assertIn('visible.Where(x => x.InvoiceKind == "Sales").Sum(x => x.RemainingAsOf)', UI)
        self.assertIn('visible.Where(x => x.InvoiceKind == "Purchase").Sum(x => x.RemainingAsOf)', UI)
    def test_partner_and_unsettled_filters(self):
        self.assertIn('partnerFilter.Text', UI)
        self.assertIn('outstandingOnly.IsChecked != true || x.RemainingAsOf > 0', UI)
    def test_export_uses_filtered_visible_rows(self):
        self.assertIn('visible.Select(x => (IReadOnlyList<object?>)', UI)
if __name__ == '__main__': unittest.main()
