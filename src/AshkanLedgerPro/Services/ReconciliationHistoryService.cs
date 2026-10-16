using System.IO;
using System.Globalization;
using AshkanLedgerPro.Data;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

// Immutable audit snapshots: never alter journal entries or bank transactions.
public sealed class ReconciliationHistoryService
{
 public sealed record Snapshot(int Id, string CreatedAtUtc, string CreatedBy, string FileName, int BankCount, int LedgerCount, int MatchedCount, int UnmatchedCount);
 public void Initialize()
 {
  using var db = new LedgerDb();
  db.Database.ExecuteSqlRaw("""CREATE TABLE IF NOT EXISTS BankReconciliationRuns (Id INTEGER PRIMARY KEY AUTOINCREMENT, CreatedAtUtc TEXT NOT NULL, CreatedBy TEXT NOT NULL, FileName TEXT NOT NULL, BankCount INTEGER NOT NULL, LedgerCount INTEGER NOT NULL, MatchedCount INTEGER NOT NULL, UnmatchedCount INTEGER NOT NULL)""");
  db.Database.ExecuteSqlRaw("""CREATE TABLE IF NOT EXISTS BankReconciliationRows (Id INTEGER PRIMARY KEY AUTOINCREMENT, RunId INTEGER NOT NULL, Reference TEXT NOT NULL, Amount TEXT NOT NULL, Status TEXT NOT NULL, Source TEXT NOT NULL, FOREIGN KEY(RunId) REFERENCES BankReconciliationRuns(Id) ON DELETE RESTRICT)""");
  // Immutable reconciliation audit snapshots: modifications are never legitimate.
  foreach (var (table, operation) in new[] {
      ("BankReconciliationRuns", "UPDATE"), ("BankReconciliationRuns", "DELETE"),
      ("BankReconciliationRows", "UPDATE"), ("BankReconciliationRows", "DELETE") })
  {
   var trigger = $"guard_{table.ToLowerInvariant()}_{operation.ToLowerInvariant()}";
   db.Database.ExecuteSqlRaw($"CREATE TRIGGER IF NOT EXISTS {trigger} BEFORE {operation} ON {table} BEGIN SELECT RAISE(ABORT, 'Immutable reconciliation history'); END");
  }

 }
 public int Save(string fileName, string user, int bankCount, int ledgerCount, IReadOnlyList<BankReconciliation.Result> rows)
 {
  if(rows.Count == 0) throw new ArgumentException("No comparison results to save.");
  using var db = new LedgerDb();
  using var transaction = db.Database.BeginTransaction();
  var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
  var matched = rows.Count(x => x.Status == "Matched");
  db.Database.ExecuteSqlInterpolated($"INSERT INTO BankReconciliationRuns (CreatedAtUtc, CreatedBy, FileName, BankCount, LedgerCount, MatchedCount, UnmatchedCount) VALUES ({now}, {user}, {Path.GetFileName(fileName)}, {bankCount}, {ledgerCount}, {matched}, {rows.Count-matched})");
  var id = db.Database.SqlQueryRaw<long>("SELECT last_insert_rowid() AS Value").AsEnumerable().Single();
  foreach(var row in rows)
  {
   var amount = row.Amount.ToString("0.00", CultureInfo.InvariantCulture);
   db.Database.ExecuteSqlInterpolated($"INSERT INTO BankReconciliationRows (RunId, Reference, Amount, Status, Source) VALUES ({id}, {row.Reference}, {amount}, {row.Status}, {row.Source})");
  }
  transaction.Commit();
  return checked((int)id);
 }
 public List<BankReconciliation.Result> GetRows(int runId)
 {
  using var db = new LedgerDb();
  var conn = db.Database.GetDbConnection();
  conn.Open();
  try
  {
   using var cmd = conn.CreateCommand();
   cmd.CommandText = "SELECT Reference, Amount, Status, Source FROM BankReconciliationRows WHERE RunId = @runId ORDER BY Id";
   var parameter = cmd.CreateParameter();
   parameter.ParameterName = "@runId";
   parameter.Value = runId;
   cmd.Parameters.Add(parameter);
   using var reader = cmd.ExecuteReader();
   var rows = new List<BankReconciliation.Result>();
   while(reader.Read())
    rows.Add(new BankReconciliation.Result(reader.GetString(0), decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture), reader.GetString(2), reader.GetString(3)));
   return rows;
  }
  finally { conn.Close(); }
 }
 public List<Snapshot> Recent()
 {
  using var db = new LedgerDb();
  var conn = db.Database.GetDbConnection();conn.Open();
  try
  {
   using var cmd = conn.CreateCommand();
   cmd.CommandText = "SELECT Id, CreatedAtUtc, CreatedBy, FileName, BankCount, LedgerCount, MatchedCount, UnmatchedCount FROM BankReconciliationRuns ORDER BY Id DESC LIMIT 100";
   using var reader = cmd.ExecuteReader();var list = new List<Snapshot>();
   while(reader.Read()) list.Add(new Snapshot(reader.GetInt32(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetInt32(4),reader.GetInt32(5),reader.GetInt32(6),reader.GetInt32(7)));
   return list;
  }
  finally {conn.Close();}
 }
}
