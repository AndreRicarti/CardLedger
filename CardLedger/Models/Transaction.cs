namespace CardLedger.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = "Não Categorizado";
        public string Source { get; set; } = "nubank";
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsRefund { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
