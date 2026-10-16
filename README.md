# Ashkan Ledger Pro — Stage 40

## Improvements
- Period report now includes opening balance, period debits and credits, and closing balance per account.
- Opening balance is the signed net of **posted** entries before the start date (debit-positive). Closing = opening + debit - credit.
- Debit/credit columns split signed opening and closing balances for presentation.
- Period XLSX exports numeric amounts as real Excel numbers while account codes remain text. Other XLSX exports continue using the safe text-only writer.
- Reports remain read-only.

## Limitations
- Opening balances are computed from the full journal history, not a separate fiscal-year opening document.
- This is a period account movement report, not a certified statutory trial balance.
- WPF compilation/runtime could not be tested here (Windows .NET SDK unavailable). Build with VS 2022 and .NET 8 Desktop workload.
- Back up `%LOCALAPPDATA%\AshkanLedgerPro\ledger.db` before upgrading.


## Stage 41 — Detailed general ledger

- New read-only account-level journal movement report, accessible from Financial period reports.
- Account selector, Jalali/Gregorian date inputs, opening balance, debit and credit movement, and running/closing balance.
- Typed XLSX export (numeric amounts; dates and identifiers remain text).
- No schema changes or financial mutations. All previous modules are preserved.
- This is **not** a fiscal-year close/open workflow. Do not use it as a substitute for audited financial statements.
- Build/runtime verification requires Windows and Visual Studio 2022 with .NET 8 desktop workload.


Stage 45: see README_STAGE45.md for SQLite fiscal lock triggers and limitations.


See README_STAGE47.md for new database protection diagnostics.


Stage 52: Windows CI workflow, source-backed SQLite guard tests, repository hygiene. See README_STAGE52.md.


Stage 65: see `README_STAGE65.md` for historical balance date filters, visual bars and printing.


Stage 88: see `README_STAGE88.md` for the new top-five account chart.


## Stage 89
Cash movement monthly chart, cumulative recorded movement, warning threshold, and CSV export. See `README_STAGE89.md`.


Stage 90: SDK roll-forward compatibility and session-only expense budget checking; see README_STAGE90.md.

### Latest update: Stage 91
Expense budget presets now persist locally (save/load/delete) without changing accounting entries. See `README_STAGE91.md`.


Stage 92: see [README_STAGE92.md](README_STAGE92.md) for the CS0165 fix and saved-budget grid.
"# ashkan-ledger-pro" 
