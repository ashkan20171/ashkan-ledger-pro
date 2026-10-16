import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class BuildDiagnosticsTests(unittest.TestCase):
    def test_binary_log_enabled(self):
        s = (ROOT / 'scripts/build-windows.ps1').read_text(encoding='utf-8')
        self.assertIn('"-bl:$binlog"', s)
        self.assertIn('artifacts/build', s)
    def test_sdk_diagnostics_saved(self):
        s = (ROOT / 'scripts/build-windows.ps1').read_text(encoding='utf-8')
        self.assertIn('dotnet --info', s)
        self.assertIn('dotnet-info.txt', s)
    def test_ci_uploads_on_failure(self):
        s = (ROOT / '.github/workflows/windows-build.yml').read_text(encoding='utf-8')
        self.assertIn('if: always()', s)
        self.assertIn('actions/upload-artifact@v4', s)
    def test_ci_reuses_build_script(self):
        s = (ROOT / '.github/workflows/windows-build.yml').read_text(encoding='utf-8')
        self.assertIn('./scripts/build-windows.ps1 -Release -SkipRestore -SkipSmoke', s)
    def test_build_still_runs_smoke_by_default(self):
        s = (ROOT / 'scripts/build-windows.ps1').read_text(encoding='utf-8')
        self.assertIn('if (-not $SkipSmoke)', s)
        self.assertIn('PdfRuntimeSmoke.csproj', s)
