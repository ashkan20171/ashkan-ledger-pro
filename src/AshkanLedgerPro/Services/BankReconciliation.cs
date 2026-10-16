using System.Globalization;
using System.IO;
using System.Text;

namespace AshkanLedgerPro.Services;

// Read-only reconciliation: no database writes or automatic posting.
public static class BankReconciliation
{
 public sealed record Entry(string Reference, decimal Amount);
 public sealed record Result(string Reference, decimal Amount, string Status, string Source);

 public static List<Entry> ReadBankCsv(string path)
 {
  var lines=File.ReadAllLines(path,Encoding.UTF8);
  if(lines.Length==0)throw new InvalidDataException("CSV is empty.");
  var header=Split(lines[0]);
  var refIndex=header.FindIndex(x=>x.Trim().TrimStart('\uFEFF').Equals("Reference",StringComparison.OrdinalIgnoreCase));
  var amountIndex=header.FindIndex(x=>x.Trim().Equals("Amount",StringComparison.OrdinalIgnoreCase));
  if(refIndex<0||amountIndex<0)throw new InvalidDataException("CSV must contain Reference and Amount columns.");
  var result=new List<Entry>();
  for(int i=1;i<lines.Length;i++)
  {
   if(string.IsNullOrWhiteSpace(lines[i]))continue;
   var cells=Split(lines[i]);
   if(cells.Count<=Math.Max(refIndex,amountIndex))throw new InvalidDataException($"Missing columns at row {i+1}.");
   if(!decimal.TryParse(cells[amountIndex],NumberStyles.Number,CultureInfo.InvariantCulture,out var amount))throw new InvalidDataException($"Invalid amount at row {i+1}. Use dot decimal separator.");
   if(decimal.Round(amount,2)!=amount)throw new InvalidDataException($"Amount has more than two decimals at row {i+1}.");
   result.Add(new Entry(cells[refIndex].Trim(),amount));
  }
  return result;
 }

 public static List<Result> Compare(IReadOnlyList<Entry> bank,IReadOnlyList<Entry> local)
 {
  var remaining=local.Select((x,i)=>(Entry:x,Index:i)).ToList();
  var result=new List<Result>();
  foreach(var row in bank)
  {
   var match=remaining.FindIndex(x=>string.Equals(x.Entry.Reference.Trim(),row.Reference.Trim(),StringComparison.OrdinalIgnoreCase)&&x.Entry.Amount==row.Amount);
   if(match>=0){remaining.RemoveAt(match);result.Add(new Result(row.Reference,row.Amount,"Matched","Bank"));}
   else result.Add(new Result(row.Reference,row.Amount,"Bank only","Bank"));
  }
  result.AddRange(remaining.Select(x=>new Result(x.Entry.Reference,x.Entry.Amount,"Ledger only","Ledger")));
  return result;
 }

 // Minimal RFC 4180 line parser (quoted commas and doubled quotes); embedded newlines are not supported.
 private static List<string> Split(string line)
 {
  var cells=new List<string>();var value=new StringBuilder();bool quoted=false;
  for(int i=0;i<line.Length;i++)
  {
   char c=line[i];
   if(c=='"')
   {
    if(quoted&&i+1<line.Length&&line[i+1]=='"'){value.Append('"');i++;}
    else quoted=!quoted;
   }
   else if(c==','&&!quoted){cells.Add(value.ToString());value.Clear();}
   else value.Append(c);
  }
  if(quoted)throw new InvalidDataException("Unclosed CSV quote.");
  cells.Add(value.ToString());return cells;
 }
}
