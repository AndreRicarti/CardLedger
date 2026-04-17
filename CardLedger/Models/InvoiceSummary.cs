namespace CardLedger.Models;

public sealed class InvoiceSummary
{
    public string InvoiceKey { get; set; } = string.Empty;
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetTotal { get; set; }
    public int TransactionCount { get; set; }
    public List<CategorySummary> Categories { get; set; } = [];
}
