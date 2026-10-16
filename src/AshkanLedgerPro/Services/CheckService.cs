using AshkanLedgerPro.Data;
using AshkanLedgerPro.Models;
using Microsoft.EntityFrameworkCore;
namespace AshkanLedgerPro.Services;
public sealed class CheckService
{
 public void Initialize()
 {
  using var db=new LedgerDb();
  db.Database.ExecuteSqlRaw("""CREATE TABLE IF NOT EXISTS "PaymentChecks" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Direction" TEXT NOT NULL, "Number" TEXT NOT NULL, "Bank" TEXT NOT NULL, "PartnerId" INTEGER NOT NULL, "Amount" TEXT NOT NULL, "DueDate" TEXT NOT NULL, "Status" TEXT NOT NULL, "CreatedAt" TEXT NOT NULL, "SettledAt" TEXT NULL, "Notes" TEXT NOT NULL, FOREIGN KEY("PartnerId") REFERENCES "Partners"("Id") ON DELETE RESTRICT)""");
  db.Database.ExecuteSqlRaw("""CREATE TABLE IF NOT EXISTS "CheckStatusHistory" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "CheckId" INTEGER NOT NULL, "PreviousStatus" TEXT NOT NULL, "NewStatus" TEXT NOT NULL, "ChangedAtUtc" TEXT NOT NULL, "ChangedBy" TEXT NOT NULL, FOREIGN KEY("CheckId") REFERENCES "PaymentChecks"("Id") ON DELETE RESTRICT)""");
  db.Database.ExecuteSqlRaw("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentChecks_Bank_Number_Direction" ON "PaymentChecks" ("Bank", "Number", "Direction")""");
 }
 public List<PaymentCheck> Recent()
 {
  using var db=new LedgerDb();
  return db.PaymentChecks.AsNoTracking().Include(x=>x.Partner).OrderBy(x=>x.DueDate).ThenBy(x=>x.Id).ToList();
 }
 public int Add(int partnerId,string direction,string number,string bank,decimal amount,DateTime dueDate,string notes)
 {
  if(direction is not ("Received" or "Issued"))throw new ArgumentException("Invalid check direction.");
  number=number.Trim();bank=bank.Trim();notes=notes.Trim();
  if(number.Length is < 1 or > 60 || bank.Length is < 1 or > 100 || notes.Length>300)throw new ArgumentException("Check number, bank or notes length invalid.");
  if(amount<=0 || decimal.Round(amount,2)!=amount)throw new ArgumentException("Amount must be positive with up to two decimal places.");
  if(dueDate.Year<2000 || dueDate.Year>2200)throw new ArgumentException("Invalid due date.");
  using var db=new LedgerDb();
  var partner=db.Partners.SingleOrDefault(x=>x.Id==partnerId) ?? throw new ArgumentException("Partner not found.");
  if(direction=="Received" && partner.Kind!="Customer" || direction=="Issued" && partner.Kind!="Supplier")throw new ArgumentException("Partner type does not match direction.");
  if(db.PaymentChecks.Any(x=>x.Bank==bank && x.Number==number && x.Direction==direction))throw new ArgumentException("Check already exists for this bank and direction.");
  var item=new PaymentCheck{PartnerId=partnerId,Direction=direction,Number=number,Bank=bank,Amount=amount,DueDate=dueDate.Date,Notes=notes};
  db.PaymentChecks.Add(item);db.SaveChanges();return item.Id;
 }
 public void SetStatus(int id,string next,string changedBy="System")
 {
  if(next is not ("Cleared" or "Bounced" or "Cancelled"))throw new ArgumentException("Invalid check status.");
  using var db=new LedgerDb();
  var item=db.PaymentChecks.SingleOrDefault(x=>x.Id==id) ?? throw new ArgumentException("Check not found.");
  if(item.Status!="Pending")throw new InvalidOperationException("Only pending checks can change status.");
  using var transaction=db.Database.BeginTransaction();
  var changedAt=DateTime.UtcNow;
  db.Database.ExecuteSqlInterpolated($"INSERT INTO CheckStatusHistory (CheckId, PreviousStatus, NewStatus, ChangedAtUtc, ChangedBy) VALUES ({id}, {item.Status}, {next}, {changedAt.ToString("O",System.Globalization.CultureInfo.InvariantCulture)}, {changedBy})");
  item.Status=next;item.SettledAt=changedAt;db.SaveChanges();transaction.Commit();
 }
 public List<CheckStatusEvent> History(int checkId)
 {
  using var db=new LedgerDb();
  var connection=db.Database.GetDbConnection();connection.Open();
  try
  {
   using var cmd=connection.CreateCommand();cmd.CommandText="SELECT PreviousStatus, NewStatus, ChangedAtUtc, ChangedBy FROM CheckStatusHistory WHERE CheckId = @id ORDER BY Id DESC";
   var param=cmd.CreateParameter();param.ParameterName="@id";param.Value=checkId;cmd.Parameters.Add(param);
   using var reader=cmd.ExecuteReader();var result=new List<CheckStatusEvent>();
   while(reader.Read())result.Add(new CheckStatusEvent(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetString(3)));
   return result;
  }
  finally{connection.Close();}
 }
}
public sealed record CheckStatusEvent(string PreviousStatus,string NewStatus,string ChangedAtUtc,string ChangedBy);

