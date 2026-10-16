# Stage 81: reproducible Windows build with durable MSBuild diagnostics.
[CmdletBinding()]
param([switch]$Release, [switch]$SkipRestore, [switch]$SkipSmoke)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $root 'AshkanLedgerPro.sln'
$artifacts = Join-Path $root 'artifacts/build'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
if (-not (Test-Path $solution)) { throw "Solution not found: $solution" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET 8 SDK is required: https://dotnet.microsoft.com/download/dotnet/8.0'
}
$config = if ($Release) { 'Release' } else { 'Debug' }
Push-Location $root
try {
    $version = & dotnet --version
    if ($LASTEXITCODE -ne 0 -or [int]($version.Split('.')[0]) -lt 8) { throw '.NET SDK 8 or newer is required.' }
    & dotnet --info | Out-File -FilePath (Join-Path $artifacts 'dotnet-info.txt') -Encoding utf8
    if (-not $SkipRestore) {
        & dotnet restore $solution --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. See terminal output.' }
    }
    $binlog = Join-Path $artifacts "wpf-$config.binlog"
    & dotnet build $solution --configuration $config --no-restore -warnaserror "-bl:$binlog"
    if ($LASTEXITCODE -ne 0) { throw "WPF build failed. Inspect $binlog using MSBuild Structured Log Viewer." }
    if (-not $SkipSmoke) {
        & dotnet run --project (Join-Path $root 'tests/PdfRuntimeSmoke/PdfRuntimeSmoke.csproj') --configuration $config
        if ($LASTEXITCODE -ne 0) { throw 'PDF runtime smoke tests failed.' }
    }
    Write-Host "Build successful ($config) using SDK $version" -ForegroundColor Green
} finally {
    Pop-Location
}
