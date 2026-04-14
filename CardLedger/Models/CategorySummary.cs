namespace CardLedger.Models
{
    public class CategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MonthlySummary
    {
        public decimal Total { get; set; }
        public List<CategorySummary> ByCategory { get; set; } = new();
    }
}
