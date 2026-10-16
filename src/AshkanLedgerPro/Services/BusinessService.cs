using AshkanLedgerPro.Data;
using AshkanLedgerPro.Models;
using Microsoft.EntityFrameworkCore;
namespace AshkanLedgerPro.Services;
public sealed class BusinessService {
 public void Initialize(){ using var db=new LedgerDb(); db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Partners (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Kind TEXT NOT NULL, Phone TEXT NOT NULL, Email TEXT NOT NULL)"); db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Stock (Id INTEGER PRIMARY KEY AUTOINCREMENT, Sku TEXT NOT NULL, Name TEXT NOT NULL, Quantity INTEGER NOT NULL, UnitPrice REAL NOT NULL)"); db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Stock_Sku ON Stock (Sku)"); db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Movements (Id INTEGER PRIMARY KEY AUTOINCREMENT, StockItemId INTEGER NOT NULL, OccurredAt TEXT NOT NULL, Type TEXT NOT NULL, Quantity INTEGER NOT NULL, Note TEXT NOT NULL, FOREIGN KEY (StockItemId) REFERENCES Stock (Id))"); }
 public List<BusinessPartner> Partners(){using var db=new LedgerDb();return db.Partners.AsNoTracking().OrderBy(x=>x.Name).ToList();}
 public void AddPartner(string name,string kind,string phone,string email){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Partner name is required.");if(kind is not ("Customer" or "Supplier"))throw new ArgumentException("Invalid partner kind.");using var db=new LedgerDb();db.Partners.Add(new BusinessPartner{Name=name.Trim(),Kind=kind,Phone=phone.Trim(),Email=email.Trim()});db.SaveChanges();}
 public List<StockItem> Stock(){using var db=new LedgerDb();return db.Stock.AsNoTracking().OrderBy(x=>x.Sku).ToList();}
 public void AddStock(string sku,string name,int quantity,decimal unitPrice){if(string.IsNullOrWhiteSpace(sku)||string.IsNullOrWhiteSpace(name))throw new ArgumentException("SKU and name are required.");if(quantity<0||unitPrice<0||decimal.Round(unitPrice,2)!=unitPrice)throw new ArgumentException("Invalid quantity or unit price.");using var db=new LedgerDb();if(db.Stock.Any(x=>x.Sku==sku.Trim()))throw new ArgumentException("SKU already exists.");db.Stock.Add(new StockItem{Sku=sku.Trim(),Name=name.Trim(),Quantity=quantity,UnitPrice=unitPrice});db.SaveChanges();}
 public List<StockMovement> Movements(){using var db=new LedgerDb();return db.Movements.AsNoTracking().Include(x=>x.StockItem).OrderByDescending(x=>x.OccurredAt).ThenByDescending(x=>x.Id).Take(500).ToList();}
 public void MoveStock(int stockId,string type,int quantity,string note){
  if(type is not ("IN" or "OUT") || quantity<=0)throw new ArgumentException("Select IN/OUT and enter a positive quantity.");
  using var db=new LedgerDb(); using var transaction=db.Database.BeginTransaction();
  var item=db.Stock.SingleOrDefault(x=>x.Id==stockId)??throw new ArgumentException("Product not found.");
  if(type=="OUT" && item.Quantity<quantity)throw new InvalidOperationException("Insufficient stock.");
  checked {item.Quantity += type=="IN" ? quantity : -quantity;}
  db.Movements.Add(new StockMovement{StockItemId=stockId,Type=type,Quantity=quantity,Note=(note??"").Trim(),OccurredAt=DateTime.UtcNow});
  db.SaveChanges();transaction.Commit();
 }
}
