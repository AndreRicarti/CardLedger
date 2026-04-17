using System.Globalization;
using CardLedger.Models;

namespace CardLedger.Services;

public interface ICsvParserService
{
    Task<List<Transaction>> ParseNubankCsvAsync(Stream fileStream, string fileName = "");
}

public sealed class CsvParserService : ICsvParserService
{
    private readonly ICategorizationService _categorizationService;

    public CsvParserService(ICategorizationService categorizationService)
    {
        _categorizationService = categorizationService;
    }

    public async Task<List<Transaction>> ParseNubankCsvAsync(
        Stream fileStream,
        string fileName = "")
    {
        var invoiceKey = ExtractInvoiceKeyFromFileName(fileName);

        var transactions = new List<Transaction>();

        using (var reader = new StreamReader(fileStream))
        {
            // Pula o header (date,title,amount)
            var header = await reader.ReadLineAsync();

            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = ParseCsvLine(line);
                if (parts.Count < 3)
                    continue;

                if (DateOnly.TryParse(parts[0], out var date) && 
                    decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    var title = parts[1];
                    var isRefund = amount < 0;
                    var category = _categorizationService.CategorizeTransaction(title);

                    transactions.Add(new Transaction
                    {
                        Date = date,
                        Title = title,
                        Amount = Math.Abs(amount),
                        Category = category,
                        Source = "nubank",
                        Year = date.Year,
                        Month = date.Month,
                        InvoiceKey = invoiceKey,
                        IsRefund = isRefund
                    });
                }
            }
        }

        return transactions;
    }

    private string ExtractInvoiceKeyFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        // Padrão esperado: Nubank_YYYY-MM-DD.csv
        // Extrai a data de vencimento da fatura
        var datePattern = @"(\d{4})-(\d{2})-(\d{2})";
        var match = System.Text.RegularExpressions.Regex.Match(fileName, datePattern);

        if (match.Success && int.TryParse(match.Groups[1].Value, out var year) &&
            int.TryParse(match.Groups[2].Value, out var month) &&
            int.TryParse(match.Groups[3].Value, out var day))
        {
            // A fatura refere-se ao mês anterior à data de vencimento
            var closingDate = new DateOnly(year, month, day);
            var invoiceDate = closingDate.AddMonths(-1);

            return $"{invoiceDate.Year}-{invoiceDate.Month:D2}";
        }

        return string.Empty;
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim('"').Trim());
                current = string.Empty;
            }
            else
            {
                current += c;
            }
        }

        result.Add(current.Trim('"').Trim());
        return result;
    }
}
