namespace CardLedger.Models
{
    public class ImportResponse
    {
        public int Imported { get; set; }
        public List<string> Months { get; set; } = new();
    }
}
