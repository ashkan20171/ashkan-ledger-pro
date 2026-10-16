using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;

namespace AshkanLedgerPro.Services;

/// <summary>
/// Minimal dependency-free PDF 1.4 report writer. Uses built-in Courier and deliberately
/// restricts PDF text to ASCII; no claim of Persian/RTL glyph support is made.
/// Monetary amounts are invariant and retain two decimal places.
/// </summary>
public static class IndependentPdfReportService
{
    public sealed record Row(string Kind, int InvoiceId, int PartnerId, decimal Total,
        decimal Allocated, decimal Remaining);

    public static void SaveHistorical(string filePath, DateTime fromDate, DateTime asOfDate,
        IEnumerable<Row> source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(source);
        if (fromDate.Date > asOfDate.Date) throw new ArgumentException("Invalid report date range.");
        var rows = source.ToList();
        if (rows.Any(r => r.InvoiceId <= 0 || r.PartnerId <= 0 || r.Total < 0 || r.Allocated < 0 || r.Remaining < 0 ||
            (r.Kind != "Sales" && r.Kind != "Purchase") || r.Allocated > r.Total || r.Remaining != r.Total - r.Allocated))
            throw new ArgumentException("Invalid financial report row.", nameof(source));
        if (rows.Count > 100000) throw new ArgumentException("Too many rows for a PDF report.");
        // Reject numbers that cannot fit the fixed-width A4 columns.
        if (rows.Any(r => new[] { r.Total, r.Allocated, r.Remaining }.Any(v => v.ToString("N2", CultureInfo.InvariantCulture).Length > 13)))
            throw new ArgumentException("A financial value exceeds the printable column width.", nameof(source));
        // Calculate summaries before writing; reject overflow without creating a partial file.
        decimal SumMoney(IEnumerable<decimal> amounts)
        {
            decimal total = 0m;
            foreach (var amount in amounts) total = checked(total + amount);
            return total;
        }
        var salesBalance = SumMoney(rows.Where(r => r.Kind == "Sales").Select(r => r.Remaining));
        var purchaseBalance = SumMoney(rows.Where(r => r.Kind == "Purchase").Select(r => r.Remaining));
        var totalBalance = SumMoney(rows.Select(r => r.Remaining));
        var pages = new List<string>();
        const int pageSize = 32;
        int count = Math.Max(1, (rows.Count + pageSize - 1) / pageSize);
        string Money(decimal x) => x.ToString("N2", CultureInfo.InvariantCulture);
        var generatedAtUtc = DateTime.UtcNow;
        for (int page = 0; page < count; page++)
        {
            var content = new StringBuilder();
            void Line(double x, double y, string text, int size = 10)
            {
                content.Append("BT /F1 ").Append(size).Append(" Tf 1 0 0 1 ")
                    .Append(x.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(y.ToString(CultureInfo.InvariantCulture)).Append(" Tm (")
                    .Append(Escape(text)).Append(") Tj ET\n");
            }
            // Branded vector header: no external image files or fonts are required.
            content.Append("0.055 0.13 0.24 rg 0 765 595 77 re f\n");
            content.Append("0.12 0.72 0.72 rg 42 779 5 42 re f\n");
            content.Append("1 1 1 rg\n");
            Line(57, 803, "ASHKAN LEDGER PRO", 16);
            Line(57, 785, "FINANCIAL INTELLIGENCE / HISTORICAL BALANCES", 9);
            content.Append("0.10 0.16 0.25 rg\n");
            Line(42, 748, "Reporting period: " + fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                + " to " + asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            content.Append("0.89 0.94 0.97 rg 38 708 520 22 re f\n0.10 0.16 0.25 rg\n");
            Line(42, 715, "Kind       Invoice  Partner       Total        Allocated       Remaining", 9);
            Line(42, 736, "Generated UTC: " + generatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), 8);
            int start = page * pageSize;
            for (int i = start; i < Math.Min(rows.Count, start + pageSize); i++)
            {
                var r = rows[i];
                var kind = r.Kind == "Sales" ? "Sales" : r.Kind == "Purchase" ? "Purchase" : "Other";
                var text = string.Format(CultureInfo.InvariantCulture,
                    "{0,-9} {1,7} {2,8} {3,13} {4,13} {5,13}",
                    kind, r.InvoiceId, r.PartnerId, Money(r.Total), Money(r.Allocated), Money(r.Remaining));
                var rowY = 689 - (i - start) * 17;
                if ((i - start) % 2 == 1)
                {
                    content.Append("0.96 0.98 0.99 rg 38 ")
                        .Append((rowY - 5).ToString(CultureInfo.InvariantCulture))
                        .Append(" 520 17 re f\n0.10 0.16 0.25 rg\n");
                }
                Line(42, rowY, text, 9);
            }
            var pageBalance = SumMoney(rows.Skip(start).Take(pageSize).Select(r => r.Remaining));
            content.Append("0.12 0.72 0.72 rg 38 111 520 2 re f\n0.10 0.16 0.25 rg\n");
            Line(42, 119, "Page subtotal: " + Money(pageBalance), 9);
            Line(42, 86, "Sales due: " + Money(salesBalance) + " | Purchase due: " + Money(purchaseBalance), 9);
            Line(42, 68, "Total rows: " + rows.Count.ToString(CultureInfo.InvariantCulture)
                + " | Total remaining: " + Money(totalBalance), 9);
            Line(42, 50, "Page " + (page + 1) + " of " + count + " | Amounts in ledger currency");
            pages.Add(content.ToString());
        }
        WritePdf(filePath, pages);
        // A direct PDF export must not leave behind a valid-looking receipt
        // for a previous version of the same filename. The audited export
        // path regenerates the receipt immediately afterwards.
        var staleReceipt = Path.GetFullPath(filePath) + ".sha256";
        if (File.Exists(staleReceipt)) File.Delete(staleReceipt);
    }

    /// <summary>
    /// Produces the PDF and a matching SHA-256 checksum receipt for audit transfer.
    /// The receipt is written through a sibling temporary file and is never generated
    /// when PDF validation or PDF writing fails.
    /// </summary>
    public static string SaveHistoricalWithChecksum(string filePath, DateTime fromDate,
        DateTime asOfDate, IEnumerable<Row> source)
    {
        SaveHistorical(filePath, fromDate, asOfDate, source);
        var fullPath = Path.GetFullPath(filePath);
        string digest;
        using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            digest = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        var receiptPath = fullPath + ".sha256";
        var temporaryReceipt = receiptPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            // sha256sum-compatible: hash, two spaces, local file name.
            File.WriteAllText(temporaryReceipt, digest + "  " + Path.GetFileName(fullPath) + "\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryReceipt, receiptPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryReceipt)) File.Delete(temporaryReceipt);
        }
        return receiptPath;
    }

    /// <summary>Checks an existing PDF against its sha256sum-compatible receipt.
    /// The receipt must refer to exactly the selected local file name. This detects
    /// corruption or alteration but does not authenticate the receipt's author.</summary>
    public static bool VerifyChecksum(string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        var fullPath = Path.GetFullPath(pdfPath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("PDF report was not found.", fullPath);
        var receiptPath = fullPath + ".sha256";
        if (!File.Exists(receiptPath)) throw new FileNotFoundException("SHA-256 receipt was not found.", receiptPath);
        // Receipts are tiny by design; reject oversized input before allocating text.
        if (new FileInfo(receiptPath).Length > 4096)
            throw new InvalidDataException("SHA-256 receipt is too large.");
        var receipt = File.ReadAllText(receiptPath, new UTF8Encoding(false, true)).TrimEnd('\r', '\n');
        // Never accept arbitrary paths from a receipt or malformed/extra lines.
        var delimiter = receipt.IndexOf("  ", StringComparison.Ordinal);
        if (delimiter != 64 || receipt.Length != delimiter + 2 + Path.GetFileName(fullPath).Length ||
            !string.Equals(receipt[(delimiter + 2)..], Path.GetFileName(fullPath), StringComparison.Ordinal) ||
            !receipt[..64].All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
            throw new InvalidDataException("Invalid or mismatched SHA-256 receipt.");
        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        // A matching checksum on an arbitrary file does not make it a PDF.
        Span<byte> header = stackalloc byte[5];
        if (stream.Read(header) != header.Length ||
            !header.SequenceEqual("%PDF-"u8))
            throw new InvalidDataException("Selected file is not a PDF document.");
        // Verify a minimal PDF trailer as well as the header. A checksum
        // alone proves byte equality, not that the bytes are a PDF.
        if (stream.Length < 16) throw new InvalidDataException("PDF file is truncated.");
        stream.Seek(-Math.Min(stream.Length, 2048L), SeekOrigin.End);
        var tail = new byte[checked((int)(stream.Length - stream.Position))];
        stream.ReadExactly(tail);
        var trailer = Encoding.ASCII.GetString(tail);
        if (!trailer.Contains("startxref", StringComparison.Ordinal) ||
            !trailer.Contains("%%EOF", StringComparison.Ordinal))
            throw new InvalidDataException("PDF trailer is missing.");
        // Require a final, well-formed trailer, not just marker strings anywhere in the file.
        // The offset must point to an actual xref table within this document.
        var match = System.Text.RegularExpressions.Regex.Match(trailer,
            @"startxref\s+(\d+)\s+%%EOF\s*\z", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (!match.Success || !long.TryParse(match.Groups[1].Value,
                NumberStyles.None, CultureInfo.InvariantCulture, out var xrefOffset) ||
            xrefOffset < 9 || xrefOffset >= stream.Length - 4)
            throw new InvalidDataException("PDF cross-reference offset or end marker is invalid.");
        stream.Position = xrefOffset;
        Span<byte> xrefHeader = stackalloc byte[4];
        if (stream.Read(xrefHeader) != xrefHeader.Length || !xrefHeader.SequenceEqual("xref"u8))
            throw new InvalidDataException("PDF cross-reference table is missing at its declared offset.");
        stream.Position = 0;
        var actual = SHA256.HashData(stream);
        var expected = Convert.FromHexString(receipt[..64]);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\")
        .Replace("(", "\\(").Replace(")", "\\)")
        .Select(ch => ch is >= ' ' and <= '~' ? ch : '?')
        .Aggregate(new StringBuilder(), (b, ch) => b.Append(ch)).ToString();

    private static void WritePdf(string path, IReadOnlyList<string> pages)
    {
        var objects = new List<byte[]>();
        void Add(string value) => objects.Add(Encoding.ASCII.GetBytes(value));
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        var kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => (4 + i * 2) + " 0 R"));
        Add("<< /Type /Pages /Kids [ " + kids + " ] /Count " + pages.Count + " >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        for (int i = 0; i < pages.Count; i++)
        {
            int pageId = 4 + i * 2, streamId = pageId + 1;
            Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents " + streamId + " 0 R >>");
            var stream = Encoding.ASCII.GetBytes(pages[i]);
            var prefix = Encoding.ASCII.GetBytes("<< /Length " + stream.Length + " >>\nstream\n");
            var suffix = Encoding.ASCII.GetBytes("\nendstream");
            var data = new byte[prefix.Length + stream.Length + suffix.Length];
            Buffer.BlockCopy(prefix, 0, data, 0, prefix.Length);
            Buffer.BlockCopy(stream, 0, data, prefix.Length, stream.Length);
            Buffer.BlockCopy(suffix, 0, data, prefix.Length + stream.Length, suffix.Length);
            objects.Add(data);
        }
        // Write to a sibling temporary file, then atomically replace the destination on success.
        // A failed export must not truncate an existing financial report.
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, ".ashkan-pdf-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            WriteToTemporary(temporaryPath, objects);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static void WriteToTemporary(string path, List<byte[]> objects)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        void Write(string text) { var bytes = Encoding.ASCII.GetBytes(text); file.Write(bytes); }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(file.Position);
            Write((i + 1) + " 0 obj\n"); file.Write(objects[i]); Write("\nendobj\n");
        }
        long xref = file.Position;
        Write("xref\n0 " + (objects.Count + 1) + "\n0000000000 65535 f \n");
        foreach (long offset in offsets.Skip(1)) Write(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        Write("trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
    }
}
