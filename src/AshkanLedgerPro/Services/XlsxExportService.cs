using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace AshkanLedgerPro.Services;

/// <summary>Writes a minimal standards-compliant XLSX workbook without Excel interop.</summary>
public static class XlsxExportService
{
    public static void Save(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> records)
    {
        if (headers.Count == 0) throw new ArgumentException("At least one column is required.", nameof(headers));
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        Add(archive, "[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
        Add(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
        Add(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Results" sheetId="1" r:id="rId1"/></sheets></workbook>""");
        Add(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        writer.WriteStartDocument(); writer.WriteStartElement("worksheet", ns); writer.WriteStartElement("sheetData", ns);
        WriteRow(headers, 1);
        int index = 2;
        foreach (var row in records) { if (index > 1048576) throw new InvalidOperationException("Excel row limit exceeded."); WriteRow(row, index++); }
        writer.WriteEndElement(); writer.WriteEndElement(); writer.WriteEndDocument();
        void WriteRow(IReadOnlyList<string?> values, int number)
        {
            writer.WriteStartElement("row", ns); writer.WriteAttributeString("r", number.ToString(System.Globalization.CultureInfo.InvariantCulture));
            for (int i = 0; i < values.Count && i < 16384; i++)
            {
                // Inline strings deliberately prevent formula execution and preserve account/check identifiers.
                writer.WriteStartElement("c", ns); writer.WriteAttributeString("r", ColumnName(i) + number); writer.WriteAttributeString("t", "inlineStr");
                writer.WriteStartElement("is", ns); writer.WriteStartElement("t", ns);
                writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                writer.WriteString(Sanitize(values[i] ?? ""));
                writer.WriteEndElement(); writer.WriteEndElement(); writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
    /// <summary>Export explicitly typed cells. Identifiers stay text; decimal amounts become numeric Excel cells.</summary>
    public static void SaveTyped(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> records)
    {
        if (headers.Count == 0) throw new ArgumentException("At least one column is required.", nameof(headers));
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        Add(archive, "[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
        Add(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
        Add(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Results" sheetId="1" r:id="rId1"/></sheets></workbook>""");
        Add(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        writer.WriteStartDocument(); writer.WriteStartElement("worksheet", ns); writer.WriteStartElement("sheetData", ns);
        WriteRow(headers.Cast<object?>().ToArray(), 1);
        int index = 2;
        foreach (var row in records) { if (index > 1048576) throw new InvalidOperationException("Excel row limit exceeded."); WriteRow(row, index++); }
        writer.WriteEndElement(); writer.WriteEndElement(); writer.WriteEndDocument();
        void WriteRow(IReadOnlyList<object?> values, int number)
        {
            writer.WriteStartElement("row", ns); writer.WriteAttributeString("r", number.ToString(System.Globalization.CultureInfo.InvariantCulture));
            for (int i = 0; i < values.Count && i < 16384; i++)
            {
                writer.WriteStartElement("c", ns); writer.WriteAttributeString("r", ColumnName(i) + number);
                if (values[i] is decimal or int or long or double or float)
                {
                    writer.WriteStartElement("v", ns);
                    writer.WriteString(Convert.ToString(values[i], System.Globalization.CultureInfo.InvariantCulture) ?? "0");
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteAttributeString("t", "inlineStr");
                    writer.WriteStartElement("is", ns); writer.WriteStartElement("t", ns);
                    writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                    writer.WriteString(Sanitize(values[i]?.ToString() ?? ""));
                    writer.WriteEndElement(); writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
    static string Sanitize(string input) => new string(input.Where(c => c == '\t' || c == '\n' || c == '\r' || (c >= ' ' && c != '\uFFFE' && c != '\uFFFF')).ToArray());
    static string ColumnName(int index) { var name = ""; for (index++; index > 0; index = (index - 1) / 26) name = (char)('A' + (index - 1) % 26) + name; return name; }
    static void Add(ZipArchive archive, string name, string content) { var e = archive.CreateEntry(name, CompressionLevel.Optimal); using var w = new StreamWriter(e.Open(), new UTF8Encoding(false)); w.Write(content); }
}
