namespace CardLedger.Models
{
    public sealed class ImportResponse
    {
        public int Imported { get; set; }
        public List<string> Months { get; set; } = new();
    }
}
