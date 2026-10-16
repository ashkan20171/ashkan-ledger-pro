using System;
using System.IO;
using System.Linq;
using System.Text;
using AshkanLedgerPro.Services;

// Runs the REAL production PDF implementation without WPF or external NuGet packages.
var folder = Path.Combine(Path.GetTempPath(), "ashkan-pdf-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}
void ExpectInvalid(Action action, string name)
{
    try { action(); }
    catch (ArgumentException) { Check(true, name); return; }
    throw new Exception("FAILED: " + name);
}
try
{
    var file = Path.Combine(folder, "historical.pdf");
    var from = new DateTime(2026, 1, 1);
    var to = new DateTime(2026, 10, 16);
    var rows = new[] {
        new IndependentPdfReportService.Row("Sales", 1, 5, 120m, 20m, 100m),
        new IndependentPdfReportService.Row("Purchase", 2, 7, 90m, 40m, 50m)
    };
    var receipt = IndependentPdfReportService.SaveHistoricalWithChecksum(file, from, to, rows);
    Check(File.Exists(file) && new FileInfo(file).Length > 200, "PDF created");
    Check(File.Exists(receipt), "SHA-256 receipt created");
    Check(IndependentPdfReportService.VerifyChecksum(file), "Valid receipt accepted");
    var pdf = File.ReadAllBytes(file);
    Check(Encoding.ASCII.GetString(pdf, 0, 8) == "%PDF-1.4", "PDF header");
    var body = Encoding.ASCII.GetString(pdf);
    Check(body.Contains("xref\n") && body.Contains("%%EOF"), "PDF cross-reference and EOF");
    Check(body.Contains("100.00") && body.Contains("50.00"), "Financial amounts present");
    File.AppendAllText(file, "tamper");
    Check(!IndependentPdfReportService.VerifyChecksum(file), "Tampered PDF rejected");
    IndependentPdfReportService.SaveHistoricalWithChecksum(file, from, to, rows);
    Check(IndependentPdfReportService.VerifyChecksum(file), "Re-export refreshes receipt");
    File.AppendAllText(receipt, "extra line");
    try { IndependentPdfReportService.VerifyChecksum(file); throw new Exception("Malformed receipt accepted"); }
    catch (InvalidDataException) { Check(true, "Malformed receipt rejected"); }
    // A PDF with a fresh checksum but without a trailer must still be rejected.
    File.WriteAllText(file, "%PDF-1.4\nnot a complete document");
    var forgedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
    File.WriteAllText(receipt, forgedHash + "  " + Path.GetFileName(file) + "\n");
    try { IndependentPdfReportService.VerifyChecksum(file); throw new Exception("Truncated PDF accepted"); }
    catch (InvalidDataException) { Check(true, "Truncated PDF rejected even with matching hash"); }
    IndependentPdfReportService.SaveHistoricalWithChecksum(file, from, to, rows);
    // Even a correctly checksummed PDF must be rejected if extra bytes follow %%EOF.
    File.AppendAllText(file, "\nappended-after-eof");
    var appendedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
    File.WriteAllText(receipt, appendedHash + "  " + Path.GetFileName(file) + "\n");
    try { IndependentPdfReportService.VerifyChecksum(file); throw new Exception("Trailing bytes accepted"); }
    catch (InvalidDataException) { Check(true, "Trailing bytes after EOF rejected"); }
    IndependentPdfReportService.SaveHistoricalWithChecksum(file, from, to, rows);
    // Forge a bogus startxref pointer while keeping the checksum up to date.
    var original = File.ReadAllText(file, Encoding.ASCII);
    var pointer = System.Text.RegularExpressions.Regex.Match(original, @"startxref\s+(\d+)\s+%%EOF");
    Check(pointer.Success, "Cross-reference pointer present");
    var forged = original[..pointer.Groups[1].Index] + "0" + original[(pointer.Groups[1].Index + pointer.Groups[1].Length)..];
    File.WriteAllText(file, forged, Encoding.ASCII);
    var forgedPointerHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
    File.WriteAllText(receipt, forgedPointerHash + "  " + Path.GetFileName(file) + "\n");
    try { IndependentPdfReportService.VerifyChecksum(file); throw new Exception("Bad xref accepted"); }
    catch (InvalidDataException) { Check(true, "Invalid cross-reference pointer rejected"); }
    IndependentPdfReportService.SaveHistoricalWithChecksum(file, from, to, rows);
    IndependentPdfReportService.SaveHistorical(file, from, to, rows);
    Check(!File.Exists(receipt), "Direct export removes stale checksum receipt");
        ExpectInvalid(() => IndependentPdfReportService.SaveHistorical(file, to, from, rows), "Invalid date range rejected");
    ExpectInvalid(() => IndependentPdfReportService.SaveHistorical(file, from, to,
        new[] { new IndependentPdfReportService.Row("Sales", 3, 5, 100m, 120m, 0m) }), "Over-allocation rejected");
    Check(!Directory.GetFiles(folder, "*.tmp").Any(), "No leftover temporary files");
    Console.WriteLine($"SUCCESS: {checks} runtime checks passed");
}
finally { Directory.Delete(folder, recursive: true); }
