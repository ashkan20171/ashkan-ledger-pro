using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AshkanLedgerPro.Models;

namespace AshkanLedgerPro.Services;

/// <summary>
/// An indicative counterparty exposure report, NOT invoice-level aging.
/// Treasury receipts/payments have no invoice allocation link in the current schema.
/// </summary>
public static class PartnerExposureService
{
    public sealed record Exposure(int PartnerId, string Partner, string Kind,
        decimal Invoiced, decimal Settled, decimal OpenBalance,
        decimal Age0To30, decimal Age31To60, decimal Age61To90, decimal AgeOver90);

    public static IReadOnlyList<Exposure> Calculate(
        IEnumerable<BusinessPartner> partners,
        IEnumerable<SalesInvoice> sales,
        IEnumerable<PurchaseInvoice> purchases,
        IEnumerable<TreasuryTransaction> treasury,
        DateTime asOf)
    {
        var cutoff = asOf.Date.AddDays(1);
        var salesRows = sales.Where(x => x.CreatedAtUtc < cutoff).ToList();
        var purchaseRows = purchases.Where(x => x.CreatedAtUtc < cutoff).ToList();
        var payments = treasury.Where(x => x.OccurredAt < cutoff).ToList();
        var result = new List<Exposure>();
        foreach (var partner in partners.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            bool customer = string.Equals(partner.Kind, "Customer", StringComparison.OrdinalIgnoreCase);
            bool supplier = string.Equals(partner.Kind, "Supplier", StringComparison.OrdinalIgnoreCase);
            if (!customer && !supplier) continue;
            var invoices = customer
                ? salesRows.Where(x => x.PartnerId == partner.Id).Select(x => (x.CreatedAtUtc, x.Total)).ToList()
                : purchaseRows.Where(x => x.PartnerId == partner.Id).Select(x => (x.CreatedAtUtc, x.Total)).ToList();
            string kind = customer ? "Receipt" : "Payment";
            decimal invoiced = invoices.Sum(x => x.Total);
            decimal settled = payments.Where(x => x.PartnerId == partner.Id &&
                string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount);
            decimal open = invoiced - settled;
            // Age bands describe ORIGINAL invoice amounts only. Without payment allocations,
            // assigning remaining balances to age bands would be misleading.
            decimal Bucket(int min, int max) => invoices.Where(x =>
                (asOf.Date - x.CreatedAtUtc.Date).Days >= min &&
                (asOf.Date - x.CreatedAtUtc.Date).Days <= max).Sum(x => x.Total);
            result.Add(new Exposure(partner.Id, partner.Name, partner.Kind, invoiced, settled, open,
                Bucket(0, 30), Bucket(31, 60), Bucket(61, 90), Bucket(91, int.MaxValue)));
        }
        return result;
    }

    public static string ToCsv(IEnumerable<Exposure> rows)
    {
        static string Q(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
        var b = new StringBuilder("PartnerId,Partner,Kind,Invoiced,Settled,IndicativeOpen,OriginalInvoices0_30,OriginalInvoices31_60,OriginalInvoices61_90,OriginalInvoicesOver90\r\n");
        foreach (var x in rows)
        {
            string N(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
            b.Append(x.PartnerId).Append(',').Append(Q(x.Partner)).Append(',').Append(Q(x.Kind)).Append(',')
                .Append(N(x.Invoiced)).Append(',').Append(N(x.Settled)).Append(',').Append(N(x.OpenBalance)).Append(',')
                .Append(N(x.Age0To30)).Append(',').Append(N(x.Age31To60)).Append(',')
                .Append(N(x.Age61To90)).Append(',').Append(N(x.AgeOver90)).Append("\r\n");
        }
        return b.ToString();
    }
}
