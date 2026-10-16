from pathlib import Path
import unittest
ROOT = Path(__file__).resolve().parents[1]
class WindowsBuildTests(unittest.TestCase):
    def test_script_uses_solution(self):
        s=(ROOT/'scripts/build-windows.ps1').read_text(encoding='utf-8')
        self.assertIn('AshkanLedgerPro.sln',s)
        self.assertIn('dotnet build',s)
    def test_script_checks_exit_code(self):
        s=(ROOT/'scripts/build-windows.ps1').read_text(encoding='utf-8')
        self.assertIn('$LASTEXITCODE -ne 0',s)
        self.assertIn('Pop-Location',s)
        self.assertIn('finally',s)
    def test_ci_runs_on_windows(self):
        s=(ROOT/'.github/workflows/windows-build.yml').read_text(encoding='utf-8')
        self.assertIn('runs-on: windows-latest',s)
        self.assertIn('build-windows.ps1',s)
    def test_ci_runs_regressions(self):
        self.assertIn('unittest discover',(ROOT/'.github/workflows/windows-build.yml').read_text(encoding='utf-8'))
    def test_sdk_rollforward(self):
        import json
        data=json.loads((ROOT/'global.json').read_text(encoding='utf-8'))
        self.assertIn(data['sdk']['rollForward'],('latestFeature','latestMajor'))
if __name__=='__main__': unittest.main()
