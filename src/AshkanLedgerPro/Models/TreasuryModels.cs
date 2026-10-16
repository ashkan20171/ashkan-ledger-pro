namespace AshkanLedgerPro.Models;
public class TreasuryTransaction
{
 public int Id { get; set; }
 public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
 public string Kind { get; set; } = "Receipt";
 public string Channel { get; set; } = "Cash";
 public int PartnerId { get; set; }
 public BusinessPartner? Partner { get; set; }
 public decimal Amount { get; set; }
 public string Reference { get; set; } = "";
 public int JournalEntryId { get; set; }
}
