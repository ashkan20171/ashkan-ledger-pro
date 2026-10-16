using AshkanLedgerPro.Data;
using AshkanLedgerPro.Models;
using Microsoft.EntityFrameworkCore;
namespace AshkanLedgerPro.Services;
public sealed class PurchaseService {
 public sealed record PurchaseItem(int StockItemId,int Quantity,decimal UnitCost);
 public void Initialize(){using var db=new LedgerDb();db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS PurchaseInvoices (Id INTEGER PRIMARY KEY AUTOINCREMENT, CreatedAtUtc TEXT NOT NULL, PartnerId INTEGER NOT NULL, Total REAL NOT NULL, FOREIGN KEY (PartnerId) REFERENCES Partners(Id))");db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS PurchaseInvoiceLines (Id INTEGER PRIMARY KEY AUTOINCREMENT, PurchaseInvoiceId INTEGER NOT NULL, StockItemId INTEGER NOT NULL, Quantity INTEGER NOT NULL, UnitCost REAL NOT NULL, LineTotal REAL NOT NULL, FOREIGN KEY (PurchaseInvoiceId) REFERENCES PurchaseInvoices(Id), FOREIGN KEY (StockItemId) REFERENCES Stock(Id))");}
 public List<PurchaseInvoice> Recent(){using var db=new LedgerDb();return db.PurchaseInvoices.AsNoTracking().Include(x=>x.Partner).Include(x=>x.Items).OrderByDescending(x=>x.Id).Take(200).ToList();}
 public int Post(int supplierId,IReadOnlyCollection<PurchaseItem> items){
  if(items==null||items.Count is <1 or >100)throw new ArgumentException("Choose 1 to 100 items.");
  if(items.Any(x=>x.Quantity<=0||x.UnitCost<0||decimal.Round(x.UnitCost,2)!=x.UnitCost))throw new ArgumentException("Invalid quantity or cost (max 2 decimals).");
  var grouped=items.GroupBy(x=>x.StockItemId).Select(g=>new PurchaseItem(g.Key,checked(g.Sum(x=>x.Quantity)),g.First().UnitCost)).ToList();
  if(items.GroupBy(x=>x.StockItemId).Any(g=>g.Select(x=>x.UnitCost).Distinct().Count()>1))throw new ArgumentException("One product cannot have different costs on this invoice.");
  using var db=new LedgerDb();using var tx=db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
  if(!db.Partners.Any(x=>x.Id==supplierId&&x.Kind=="Supplier"))throw new ArgumentException("Select a supplier.");
  var ids=grouped.Select(x=>x.StockItemId).ToList();var products=db.Stock.Where(x=>ids.Contains(x.Id)).ToDictionary(x=>x.Id);
  if(products.Count!=grouped.Count)throw new ArgumentException("Unknown product.");
  var invoice=new PurchaseInvoice{PartnerId=supplierId};
  foreach(var line in grouped){var product=products[line.StockItemId];product.Quantity=checked(product.Quantity+line.Quantity);var total=checked(line.UnitCost*line.Quantity);invoice.Items.Add(new PurchaseInvoiceLine{StockItemId=line.StockItemId,Quantity=line.Quantity,UnitCost=line.UnitCost,LineTotal=total});invoice.Total=checked(invoice.Total+total);}
  db.PurchaseInvoices.Add(invoice);db.SaveChanges();
  foreach(var line in grouped)db.Movements.Add(new StockMovement{StockItemId=line.StockItemId,Type="IN",Quantity=line.Quantity,Note=$"Purchase invoice #{invoice.Id}",OccurredAt=DateTime.UtcNow});
  db.SaveChanges();
  AutomaticPosting.Purchase(db, invoice);
  db.SaveChanges();tx.Commit();return invoice.Id;
 }
}
