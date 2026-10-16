using AshkanLedgerPro.Data;
using AshkanLedgerPro.Models;
using Microsoft.EntityFrameworkCore;
namespace AshkanLedgerPro.Services;
public sealed class TreasuryService
{
 public void Initialize()
 {
  using var db = new LedgerDb();
  db.Database.ExecuteSqlRaw("""CREATE TABLE IF NOT EXISTS "TreasuryTransactions" ("Id" INTEGER NOT NULL CONSTRAINT "PK_TreasuryTransactions" PRIMARY KEY AUTOINCREMENT, "OccurredAt" TEXT NOT NULL, "Kind" TEXT NOT NULL, "Channel" TEXT NOT NULL, "PartnerId" INTEGER NOT NULL, "Amount" TEXT NOT NULL, "Reference" TEXT NOT NULL, "JournalEntryId" INTEGER NOT NULL, FOREIGN KEY("PartnerId") REFERENCES "Partners"("Id") ON DELETE RESTRICT)""");
 }
 public List<TreasuryTransaction> Recent()
 {
  using var db = new LedgerDb();
  return db.TreasuryTransactions.AsNoTracking().Include(x=>x.Partner).OrderByDescending(x=>x.Id).Take(300).ToList();
 }
 public int Record(int partnerId,string kind,string channel,decimal amount,string reference)
 {
  if(kind is not ("Receipt" or "Payment")) throw new ArgumentException("Invalid transaction kind.");
  if(channel is not ("Cash" or "Bank")) throw new ArgumentException("Invalid channel.");
  if(amount<=0 || decimal.Round(amount,2)!=amount) throw new ArgumentException("Amount must be positive with at most two decimal places.");
  if(reference.Trim().Length>160) throw new ArgumentException("Reference too long.");
  using var db = new LedgerDb();
  var partner=db.Partners.SingleOrDefault(x=>x.Id==partnerId) ?? throw new ArgumentException("Partner not found.");
  if(kind=="Receipt" && partner.Kind!="Customer" || kind=="Payment" && partner.Kind!="Supplier") throw new ArgumentException("Choose a matching customer or supplier.");
  var liquidCode=channel=="Cash"?"1000":"1100";
  var counterCode=kind=="Receipt"?"1200":"2000";
  FiscalPeriodService.EnsurePostingAllowed(db,DateTime.Today);using var tx=db.Database.BeginTransaction();
  Account GetAccount(string code,string name,string type)
  {
   var a=db.Accounts.SingleOrDefault(x=>x.Code==code);
   if(a!=null){if(a.Type!=type)throw new InvalidOperationException("Account type mismatch: "+code);return a;}
   a=new Account{Code=code,Name=name,Type=type};db.Accounts.Add(a);return a;
  }
  var liquid=GetAccount(liquidCode,channel,"Asset");
  var counter=GetAccount(counterCode,kind=="Receipt"?"Accounts Receivable":"Accounts Payable",kind=="Receipt"?"Asset":"Liability");
  var entry=new JournalEntry{Description=$"{kind} - {partner.Name} - {reference.Trim()}",Posted=true};
  entry.Lines.Add(new JournalLine{Account=kind=="Receipt"?liquid:counter,Debit=amount});
  entry.Lines.Add(new JournalLine{Account=kind=="Receipt"?counter:liquid,Credit=amount});
  db.Entries.Add(entry);
  db.SaveChanges();
  var record=new TreasuryTransaction{PartnerId=partnerId,Kind=kind,Channel=channel,Amount=amount,Reference=reference.Trim(),JournalEntryId=entry.Id};
  db.TreasuryTransactions.Add(record);db.SaveChanges();tx.Commit();return record.Id;
 }
}
