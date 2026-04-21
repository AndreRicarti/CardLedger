using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CardLedger.Models;

public sealed class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }

    [JsonIgnore]
    public Category? CategoryEntity { get; set; }

    [NotMapped]
    public string Category
    {
        get => CategoryEntity?.Name ?? _categoryName;
        set => _categoryName = value;
    }

    [NotMapped]
    public string CategoryName
    {
        get => _categoryName;
        set => _categoryName = value;
    }

    private string _categoryName = "Não Categorizado";

    public string Source { get; set; } = "nubank";
    public int Year { get; set; }
    public int Month { get; set; }
    public string InvoiceKey { get; set; } = string.Empty;
    public bool IsRefund { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}