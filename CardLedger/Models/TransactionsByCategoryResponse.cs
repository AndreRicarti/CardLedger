namespace CardLedger.Models;

public sealed class TransactionsByCategoryResponse
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public List<Transaction> Transactions { get; set; } = [];
}
