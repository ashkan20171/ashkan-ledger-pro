using AshkanLedgerPro.Data;
using AshkanLedgerPro.Models;
using Microsoft.EntityFrameworkCore;
namespace AshkanLedgerPro.Services;
public sealed class InvoiceService {
 public void Initialize(){using var db=new LedgerDb(); db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS SalesInvoices (Id INTEGER PRIMARY KEY AUTOINCREMENT, CreatedAtUtc TEXT NOT NULL, PartnerId INTEGER NOT NULL, Status TEXT NOT NULL, Subtotal REAL NOT NULL, Discount REAL NOT NULL, Tax REAL NOT NULL, Total REAL NOT NULL, FOREIGN KEY (PartnerId) REFERENCES Partners(Id))");db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS SalesInvoiceLines (Id INTEGER PRIMARY KEY AUTOINCREMENT, SalesInvoiceId INTEGER NOT NULL, StockItemId INTEGER NOT NULL, Quantity INTEGER NOT NULL, UnitPrice REAL NOT NULL, LineTotal REAL NOT NULL, FOREIGN KEY (SalesInvoiceId) REFERENCES SalesInvoices(Id), FOREIGN KEY (StockItemId) REFERENCES Stock(Id))");}
 public List<SalesInvoice> Recent(){using var db=new LedgerDb();return db.SalesInvoices.AsNoTracking().Include(x=>x.Partner).Include(x=>x.Items).ThenInclude(x=>x.StockItem).OrderByDescending(x=>x.Id).Take(200).ToList();}
 public sealed record SaleItem(int StockItemId, int Quantity);
 public int PostSale(int partnerId, int stockItemId, int quantity, decimal discountPercent, decimal taxPercent)
  => PostSaleItems(partnerId, new[]{new SaleItem(stockItemId, quantity)}, discountPercent, taxPercent);
 public int PostSaleItems(int partnerId, IReadOnlyCollection<SaleItem> items, decimal discountPercent, decimal taxPercent){
  if(items is null || items.Count==0 || items.Count>100)throw new ArgumentException("Select between 1 and 100 invoice lines.");
  if(discountPercent<0 || discountPercent>100 || taxPercent<0 || taxPercent>100)throw new ArgumentException("Discount and tax must be between 0 and 100.");
  if(items.Any(x=>x.Quantity<=0))throw new ArgumentException("All quantities must be positive.");
  var grouped=items.GroupBy(x=>x.StockItemId).Select(x=>new SaleItem(x.Key,checked(x.Sum(y=>y.Quantity)))).ToList();
  using var db=new LedgerDb();using var tx=db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
  var partner=db.Partners.SingleOrDefault(x=>x.Id==partnerId && x.Kind=="Customer")??throw new ArgumentException("Select a customer.");
  var ids=grouped.Select(x=>x.StockItemId).ToList();
  var products=db.Stock.Where(x=>ids.Contains(x.Id)).ToDictionary(x=>x.Id);
  if(products.Count!=grouped.Count)throw new ArgumentException("One or more products no longer exist.");
  var lines=new List<SalesInvoiceLine>();
  foreach(var line in grouped){
   var product=products[line.StockItemId];
   if(product.Quantity<line.Quantity)throw new InvalidOperationException($"Insufficient stock: {product.Name}.");
   if(product.UnitPrice<0)throw new InvalidOperationException("Product price cannot be negative.");
   var amount=decimal.Round(checked(product.UnitPrice*line.Quantity),2,MidpointRounding.AwayFromZero);
   lines.Add(new SalesInvoiceLine{StockItemId=product.Id,Quantity=line.Quantity,UnitPrice=product.UnitPrice,LineTotal=amount});
  }
  var subtotal=lines.Sum(x=>x.LineTotal);
  var discount=decimal.Round(subtotal*discountPercent/100m,2,MidpointRounding.AwayFromZero);
  var tax=decimal.Round((subtotal-discount)*taxPercent/100m,2,MidpointRounding.AwayFromZero);
  var invoice=new SalesInvoice{PartnerId=partner.Id,Subtotal=subtotal,Discount=discount,Tax=tax,Total=subtotal-discount+tax,Items=lines};
  foreach(var line in grouped)products[line.StockItemId].Quantity-=line.Quantity;
  db.SalesInvoices.Add(invoice);db.SaveChanges();
  foreach(var line in grouped)db.Movements.Add(new StockMovement{StockItemId=line.StockItemId,Type="OUT",Quantity=line.Quantity,Note=$"Sales invoice #{invoice.Id}",OccurredAt=DateTime.UtcNow});
  db.SaveChanges();
  AutomaticPosting.Sale(db, invoice);
  db.SaveChanges();tx.Commit();return invoice.Id;
 }
}
