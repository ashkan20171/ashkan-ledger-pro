# Ashkan Ledger Pro

**A bilingual, desktop-first accounting platform built with C# / .NET 8 and WPF.**

[![Windows WPF CI](https://github.com/ashkan20171/ashkan-ledger-pro/actions/workflows/windows-ci.yml/badge.svg)](https://github.com/ashkan20171/ashkan-ledger-pro/actions/workflows/windows-ci.yml) ![.NET 8](https://img.shields.io/badge/.NET-8-512BD4) ![WPF](https://img.shields.io/badge/UI-WPF-0078D4) ![SQLite](https://img.shields.io/badge/Database-SQLite-003B57) ![License](https://img.shields.io/badge/license-not%20specified-lightgrey)

## Overview

Ashkan Ledger Pro is a hands-on engineering portfolio project exploring financial workflows, local-first desktop architecture, bilingual interfaces (English/Persian), role-based access, and auditable fiscal controls. It is **not represented as a certified ERP or production-ready financial system**.

## Highlights

- **Accounting:** chart of accounts, balanced journal entries, general ledger, trial balance and profit-and-loss reporting.
- **Business workflows:** customers and suppliers, inventory catalogue, stock movements, sales and purchase invoices, treasury, checks and bank reconciliation.
- **Reporting:** period-based reports, CSV and XLSX export.
- **Access control:** Admin, Accountant and Viewer roles, password hashing with PBKDF2-SHA256.
- **Fiscal safeguards:** SQLite triggers for selected locked-period operations, fiscal-period audit history and diagnostics.
- **Security events:** separate SQLite event journal, Admin-only event viewer, filtering and CSV export.
- **Localization:** Persian RTL and English LTR support, with Jalali/Gregorian date input in supported screens.
- **UI:** WPF desktop shell with light and dark themes.

## Architecture and technologies

| Layer | Technology |
| --- | --- |
| Desktop UI | WPF, XAML, C# |
| Runtime | .NET 8 (Windows) |
| Persistence | SQLite, Entity Framework Core |
| Security | Role-based access, PBKDF2-SHA256, parameterized SQL |
| Validation | Python SQLite guard tests; source-backed stock tests; Windows GitHub Actions build and XAML structure checks |

Source lives in `src/AshkanLedgerPro/`; incremental release notes are in `README_STAGE*.md`.

## Getting started

1. Install **Visual Studio 2022** with the **.NET desktop development** workload and the **.NET 8 SDK**.
2. Open `AshkanLedgerPro.sln` and restore NuGet packages.
3. Build and run the `AshkanLedgerPro` startup project on Windows.
4. Back up `%LOCALAPPDATA%\AshkanLedgerPro\ledger.db` before upgrades. The event journal is stored alongside it as `security-events.db`.
5. Run the complete SQLite regression suite: `python -m unittest discover -s tests -p "test_*.py" -v`.

> **Verification status:** The GitHub Actions workflow is supplied but has not yet been executed on GitHub. Python SQLite tests run locally; a successful end-to-end Windows WPF build is **not** claimed. Review CI results and validate locally before deployment.

## Engineering trade-offs and roadmap

This project intentionally keeps data local. Trigger-based safeguards are defense in depth, not immutable audit logs. Future improvements include stronger transactional integration tests, complete localization, migration management, more comprehensive stock and bank integrity checks, and automated Windows CI builds.

## For reviewers

The most interesting areas to inspect are `Services/FiscalPeriodService.cs`, `Services/SecurityEventService.cs`, financial posting services, and the `tests/` directory. The project demonstrates practical trade-offs in desktop application design, accounting data integrity and bilingual user experience.

## License

No open-source license is granted by default. Add an explicit `LICENSE` file before offering reuse rights.

## Continuous integration

The repository includes `.github/workflows/windows-ci.yml` for Windows Release builds, SQLite regression tests and XAML XML checks. After pushing to GitHub, open **Actions → Windows WPF CI** to inspect results. CI failures must be resolved before presenting a passing-build claim.

## Stage 53: Annual management analytics

The dashboard now includes a read-only annual revenue/expense/net-profit table with a year selector and CSV export. Calculations include posted journal entries only; amounts are not currency-converted. See `README_STAGE53.md`.

## Stage 54: Treasury cash-flow analytics

The overview includes a bilingual, read-only 12-month treasury cash movement report with CSV export. It separates receipts and payments by cash/bank channel and deliberately distinguishes cash movement from accrual profitability. It is **not** an audited IFRS cash flow statement.

## Stage 55: Year-over-year analysis

The bilingual WPF dashboard now compares monthly posted-journal profitability across two consecutive Gregorian years, including absolute/percentage changes and CSV export. This is a read-only operational analysis, not an audited financial statement.

## Stage 56: indicative counterparty exposure

The Overview dashboard now includes as-of-date counterparty balances and original-invoice age distribution with CSV export. **Not invoice-level AR/AP aging:** treasury transactions are not allocated to invoices. See `README_STAGE56.md` for limitations.


### Stage 57: invoice allocations
Explicitly link posted treasury receipts/payments to sales or purchase invoices, with partner and amount validation, immutable allocation records, and per-invoice remaining balances. Aging uses invoice date, not contractual due date. See `README_STAGE57.md` for limitations.


### Stage 58 — Due-date-aware receivables
Contractual due-date entry and invoice-level overdue buckets calculated from outstanding allocated balances. Missing due dates are explicitly flagged. See `README_STAGE58.md` for limitations.

## Stage 59 — Overdue monitoring and due-date history
Read-only overdue receivables/payables overview, CSV export, and transactional due-date change audit. The report uses current recorded allocations and is not a historical IFRS statement.

### Stage 60: due-date accountability
The invoice due-date audit now records the authenticated user, and the workspace provides an on-demand seven-day upcoming due-date view. Existing SQLite audit rows migrate without data loss.


### Stage 61: Audited settlement corrections
Controlled allocation reversals with reason, actor, UTC timestamp, immutable SQLite audit rows, and restored available balances. See `README_STAGE61.md` for limitations.

## Stage 62: allocation audit
Read-only reconciliation audit with reversal attribution and CSV export. This does not reconstruct historical as-of balances.

### Stage 63 — Historical balances
Read-only invoice allocation snapshot at a selected date, honoring allocation reversal timestamps, with XLSX export. See README_STAGE63.md for limitations.

### Stage 64 — Historical finance dashboard
Historical receivables/payables KPI ribbon, partner filters, unsettled-only view, and typed XLSX export. See `README_STAGE64.md`.


### Stage 65 — Financial analytics and printing
Historical balances now support invoice-date range filtering, proportional receivable/payable bars and paginated WPF print output. See `README_STAGE65.md`.

### Stage 66 — Historical monthly invoice trend
Native WPF sales/purchase trend lines, filtered consistently with the historical snapshot; Windows Print to PDF workflow. See `README_STAGE66.md`.


### Stage 67
Responsive monthly finance chart and enhanced printable audit report (localized headings, totals and UTC timestamp). See `README_STAGE67.md`.

## Stage 68 — Driver-free PDF export
Historical balance reports can now be exported directly to multi-page PDF 1.4 (English/ASCII text), without Windows Print to PDF. See `README_STAGE68.md` for limitations.

### Stage 69 — PDF hardening
Improved A4 pagination, financial value validation, and fixed-width PDF column alignment. Standalone PDF remains ASCII-only pending proper Unicode font embedding and bidi support.


### Stage 70 — Compiler hotfix
Resolved undefined `NewDb()` references and missing `System.IO` imports. See `README_STAGE70.md`.

## Stage 71 — Safer financial PDF exports
Atomic PDF replacement, strict financial arithmetic validation, checked totals and sales/purchase summaries. See `README_STAGE71.md`.


### Stage 72
RTL-aware Windows print reports for Persian and page-subtotal auditing for the standalone ASCII PDF. See `README_STAGE72.md`.


## Stage 73 — Branded PDF reports
Vector-branded navy/teal report header, zebra-striped rows and PDF layout regression tests. Independent PDF output remains ASCII-only.


### Stage 74 — Auditable PDF export
Historical PDF exports now produce a companion SHA-256 receipt (`.pdf.sha256`) for integrity verification. This is a checksum, not a cryptographic signature. Independent Persian/RTL PDF is still a future enhancement.

### Stage 75 — Verify PDF integrity
Read-only PDF SHA-256 verification against the adjacent receipt, including strict receipt format and filename checks. See `README_STAGE75.md`.

## Stage 76: PDF receipt verification hardening
Bounded UTF-8 receipt parsing, PDF signature check and fixed-time SHA-256 comparison. See `README_STAGE76.md`.


## Stage 77 — Windows build validation

Run `powershell -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -Release` on Windows. GitHub Actions compiles WPF on `windows-latest` and runs Python regression tests. See `README_STAGE77.md`.

## Stage 78: Real PDF runtime smoke tests
The Windows CI builds the WPF app and runs a separate .NET 8 console smoke test against the actual production PDF writer source. Run `dotnet run --project tests/PdfRuntimeSmoke/PdfRuntimeSmoke.csproj` locally.

## Stage 79 — PDF integrity hardening
Direct exports invalidate stale checksum receipts; verifier checks PDF trailer markers. See `README_STAGE79.md`.

## Stage 80
Strict PDF trailer and cross-reference pointer verification, with C# runtime smoke regression cases. See `README_STAGE80.md`.


## Stage 81 — Windows build diagnostics
PowerShell build script records MSBuild binary logs and .NET SDK details; CI uploads them on failures. See `README_STAGE81.md`.

### Stage 82
Persian/RTL Unicode historical-balance PDF via Windows Microsoft Print to PDF; audited ASCII PDF export remains unchanged. See `README_STAGE82.md`.


### Stage 83 — Persian print layout
Historical invoice reports now include branded header, striped rows, and bilingual page numbering via a WPF paginator. Windows PDF printer required; no receipt on that path.

## Stage 84 — Dashboard KPIs
Read-only profit margin and six-month performance cards, plus dashboard refresh. Zero revenue shows N/A rather than a misleading percentage.

## Stage 85 — Period comparison
Compare any selected Gregorian date range with the immediately preceding equal-length period; export UTF-8 CSV. Read-only posted journal metrics.

## Stage 86
Localized Jalali/Gregorian date filters, comparative dashboard bars, and growth metrics. See README_STAGE86.md.

## Stage 87 — Account drilldown
Dashboard breakdown by posted Revenue/Expense account, date-filtered with safe CSV export.


### Stage 88 — Top-five account contributions
Dashboard account drilldown now includes bilingual signed top-five horizontal bars and share-of-absolute-total labels.


## Stage 89
Cash movement monthly chart, cumulative recorded movement, warning threshold, and CSV export. See `README_STAGE89.md`.


Stage 90: SDK roll-forward compatibility and session-only expense budget checking; see README_STAGE90.md.

## Stage 91 — Persistent expense budgets

Expense budget presets can be saved, loaded and deleted for a selected expense account and date range. The local JSON file is separate from accounting entries, and save operations use a temporary file and replacement. This is a local planning aid, not an audited budgeting subsystem. See `README_STAGE91.md`.
