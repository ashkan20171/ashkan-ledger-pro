using System.Globalization;
using System.IO;
using System.Text.Json;

namespace AshkanLedgerPro.Services;

/// <summary>Local, non-ledger budget presets. Never modifies accounting postings.</summary>
public sealed class BudgetPresetStore
{
 public sealed record Preset(int AccountId, DateTime From, DateTime To, decimal Limit);
 readonly string path;
 static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
 public BudgetPresetStore(string? filePath = null)
 {
  path = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
   "AshkanLedgerPro", "budget-presets.json");
 }
 public IReadOnlyList<Preset> Load()
 {
  if (!File.Exists(path)) return Array.Empty<Preset>();
  var info = new FileInfo(path);
  if (info.Length > 1024 * 1024) throw new InvalidDataException("Budget preset file is too large.");
  var rows = JsonSerializer.Deserialize<List<Preset>>(File.ReadAllText(path), JsonOptions)
   ?? throw new InvalidDataException("Budget preset file is empty or invalid.");
  if (rows.Count > 10000) throw new InvalidDataException("Too many budget presets.");
  foreach (var row in rows) Validate(row);
  if (rows.GroupBy(x => (x.AccountId, x.From.Date, x.To.Date)).Any(g => g.Count() > 1))
   throw new InvalidDataException("Duplicate budget preset keys.");
  return rows;
 }
 public void Save(Preset preset)
 {
  Validate(preset);
  var rows = Load().ToList();
  rows.RemoveAll(x => SameKey(x, preset));
  rows.Add(preset);
  Write(rows);
 }
 public bool Delete(int accountId, DateTime from, DateTime to)
 {
  var key = new Preset(accountId, from.Date, to.Date, 0m);
  Validate(key);
  var rows = Load().ToList();
  var removed = rows.RemoveAll(x => SameKey(x, key)) > 0;
  if (removed) Write(rows);
  return removed;
 }
 static bool SameKey(Preset a, Preset b) => a.AccountId == b.AccountId &&
  a.From.Date == b.From.Date && a.To.Date == b.To.Date;
 static void Validate(Preset row)
 {
  if (row.AccountId <= 0 || row.From == default || row.To == default ||
      row.From.TimeOfDay != TimeSpan.Zero || row.To.TimeOfDay != TimeSpan.Zero ||
      row.From > row.To || row.Limit < 0m || row.Limit > 1_000_000_000_000_000m)
   throw new InvalidDataException("Invalid budget preset.");
 }
 void Write(List<Preset> rows)
 {
  if (rows.Count > 10000) throw new InvalidDataException("Too many budget presets.");
  var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
  Directory.CreateDirectory(directory);
  var temp = Path.Combine(directory, ".budget-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp");
  try
  {
   File.WriteAllText(temp, JsonSerializer.Serialize(rows, JsonOptions));
   File.Move(temp, path, overwrite: true);
  }
  finally { if (File.Exists(temp)) File.Delete(temp); }
 }
}
