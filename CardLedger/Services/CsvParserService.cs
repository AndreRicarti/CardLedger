using CardLedger.Helpers;
using CardLedger.Models;

namespace CardLedger.Services;

public interface ICsvParserService
{
    Task<List<Transaction>> ParseNubankCsvAsync(Stream fileStream, string fileName = "");
}

public sealed class CsvParserService(ICategorizationService categorizationService) : ICsvParserService
{
    public async Task<List<Transaction>> ParseNubankCsvAsync(
        Stream fileStream,
        string fileName = "")
    {
        var invoiceKey = ExtractInvoiceKeyFromFileName(fileName);

        var transactions = new List<Transaction>();

        using var reader = new StreamReader(fileStream);

        await reader.ReadLineAsync();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = ParseCsvLine(line);
            if (parts.Count < 3)
                continue;

            if (!DateOnly.TryParse(parts[0], out var date) ||
                !DecimalParser.TryParseAmount(parts[2], out var amount)) continue;

            var title = parts[1];

            if (title.Equals("Pagamento recebido", StringComparison.OrdinalIgnoreCase))
                continue;

            var isRefund = amount < 0;

            var category = categorizationService.CategorizeTransaction(title);

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

        return transactions;
    }

    private static string ExtractInvoiceKeyFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        const string datePattern = @"(\d{4})-(\d{2})-(\d{2})";
        var match = System.Text.RegularExpressions.Regex.Match(fileName, datePattern);

        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var year) ||
            !int.TryParse(match.Groups[2].Value, out var month) ||
            !int.TryParse(match.Groups[3].Value, out var day)) return string.Empty;

        var closingDate = new DateOnly(year, month, day);

        var invoiceDate = closingDate.AddMonths(-1);

        return $"{invoiceDate.Year}-{invoiceDate.Month:D2}";

    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        foreach (var c in line)
        {
            switch (c)
            {
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case ',' when !inQuotes:
                    result.Add(current.Trim('"').Trim());
                    current = string.Empty;
                    break;
                default:
                    current += c;
                    break;
            }
        }

        result.Add(current.Trim('"').Trim());

        return result;
    }
}
