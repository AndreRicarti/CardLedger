namespace CardLedger.Models;

public sealed class MonthlyInvoice
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string InvoiceKey { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetTotal { get; set; }
    public int TransactionCount { get; set; }
}
