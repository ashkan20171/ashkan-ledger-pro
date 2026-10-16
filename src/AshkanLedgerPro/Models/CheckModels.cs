namespace AshkanLedgerPro.Models;
public sealed class PaymentCheck
{
 public int Id { get; set; }
 public string Direction { get; set; } = "Received";
 public string Number { get; set; } = "";
 public string Bank { get; set; } = "";
 public int PartnerId { get; set; }
 public BusinessPartner? Partner { get; set; }
 public decimal Amount { get; set; }
 public DateTime DueDate { get; set; }
 public string Status { get; set; } = "Pending";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public DateTime? SettledAt { get; set; }
 public string Notes { get; set; } = "";
}
