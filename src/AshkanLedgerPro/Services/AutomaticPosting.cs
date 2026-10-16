using AshkanLedgerPro.Data;
using AshkanLedgerPro.Models;
using Microsoft.EntityFrameworkCore;

namespace AshkanLedgerPro.Services;

/// <summary>Posts balanced invoice journal entries inside the caller's database transaction.</summary>
internal static class AutomaticPosting
{
    private static Account Account(LedgerDb db, string code, string name, string type)
    {
        var account = db.Accounts.SingleOrDefault(a => a.Code == code);
        if (account is not null)
        {
            if (account.Type != type)
                throw new InvalidOperationException($"Account {code} must be of type {type}.");
            return account;
        }
        account = new Account { Code = code, Name = name, Type = type };
        db.Accounts.Add(account);
        return account;
    }

    private static void Post(LedgerDb db, string description,
        params (Account Account, decimal Debit, decimal Credit)[] lines)
    {
        if (lines.Any(x => x.Debit < 0 || x.Credit < 0 ||
            (x.Debit > 0 && x.Credit > 0)))
            throw new InvalidOperationException("Invalid journal line.");
        var debit = lines.Sum(x => x.Debit);
        var credit = lines.Sum(x => x.Credit);
        if (debit <= 0 || debit != credit)
            throw new InvalidOperationException("Invoice journal is not balanced.");
        FiscalPeriodService.EnsurePostingAllowed(db,DateTime.Today);var entry = new JournalEntry { Description = description, Posted = true };
        foreach (var line in lines.Where(x => x.Debit != 0 || x.Credit != 0))
            entry.Lines.Add(new JournalLine
            {
                Account = line.Account, Debit = line.Debit, Credit = line.Credit
            });
        db.Entries.Add(entry);
    }

    internal static void Sale(LedgerDb db, SalesInvoice invoice)
    {
        var receivable = Account(db, "1200", "Accounts Receivable", "Asset");
        var revenue = Account(db, "4000", "Revenue", "Revenue");
        var tax = Account(db, "2100", "Sales Tax Payable", "Liability");
        var netSales = invoice.Subtotal - invoice.Discount;
        Post(db, $"Sales invoice #{invoice.Id}",
            (receivable, invoice.Total, 0m),
            (revenue, 0m, netSales),
            (tax, 0m, invoice.Tax));
    }

    internal static void Purchase(LedgerDb db, PurchaseInvoice invoice)
    {
        var inventory = Account(db, "1300", "Inventory", "Asset");
        var payable = Account(db, "2000", "Accounts Payable", "Liability");
        Post(db, $"Purchase invoice #{invoice.Id}",
            (inventory, invoice.Total, 0m),
            (payable, 0m, invoice.Total));
    }
}
