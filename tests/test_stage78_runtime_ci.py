import pathlib
import unittest
ROOT = pathlib.Path(__file__).resolve().parents[1]
class Stage78RuntimeTests(unittest.TestCase):
    def test_links_real_production_pdf_source(self):
        self.assertIn('IndependentPdfReportService.cs', (ROOT/'tests/PdfRuntimeSmoke/PdfRuntimeSmoke.csproj').read_text())
    def test_ci_executes_runtime(self):
        self.assertIn('dotnet run --project tests/PdfRuntimeSmoke/PdfRuntimeSmoke.csproj', (ROOT/'.github/workflows/windows-build.yml').read_text())
    def test_windows_script_executes_runtime(self):
        self.assertIn('PdfRuntimeSmoke.csproj', (ROOT/'scripts/build-windows.ps1').read_text())
    def test_smoke_covers_tamper_and_financial_validation(self):
        s=(ROOT/'tests/PdfRuntimeSmoke/Program.cs').read_text()
        for phrase in ('Tampered PDF rejected','Over-allocation rejected','Malformed receipt rejected','PDF cross-reference and EOF'):
            self.assertIn(phrase,s)
if __name__=='__main__': unittest.main()
