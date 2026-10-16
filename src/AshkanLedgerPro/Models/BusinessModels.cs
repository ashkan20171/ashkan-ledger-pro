namespace AshkanLedgerPro.Models;
public class BusinessPartner { public int Id { get; set; } public string Name { get; set; } = ""; public string Kind { get; set; } = "Customer"; public string Phone { get; set; } = ""; public string Email { get; set; } = ""; }
public class StockItem { public int Id { get; set; } public string Sku { get; set; } = ""; public string Name { get; set; } = ""; public int Quantity { get; set; } public decimal UnitPrice { get; set; } }

public class StockMovement { public int Id { get; set; } public int StockItemId { get; set; } public StockItem? StockItem { get; set; } public DateTime OccurredAt { get; set; } public string Type { get; set; } = "IN"; public int Quantity { get; set; } public string Note { get; set; } = ""; }
