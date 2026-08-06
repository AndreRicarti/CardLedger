using System.Text.RegularExpressions;
using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Services;

public interface IInvoiceService
{
    Task<InvoiceSummary?> GetInvoiceSummaryByKeyAsync(string invoiceKey);
    Task<List<TransactionsByCategoryResponse>?> GetTransactionsByCategoryAsync(string invoiceKey, string? category = null);
    Task<int> ImportTransactionsAsync(List<Transaction> transactions);
}

public sealed class InvoiceService(InvoiceDbContext context) : IInvoiceService
{
    private static readonly Regex InstallmentTitleRegex = new(
        @"^(?<base>.+?)\s*-\s*Parcela\s+(?<atual>\d+)\s*/\s*(?<total>\d+)\s*(\((?<desc>.+)\))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<InvoiceSummary?> GetInvoiceSummaryByKeyAsync(string invoiceKey)
    {
        var transactions = await context.Transactions
            .Include(t => t.CategoryEntity)
            .Where(t => t.InvoiceKey == invoiceKey)
            .ToListAsync();

        if (!transactions.Any())
            return null;

        var parts = invoiceKey.Split('-');
        var year = int.TryParse(parts[0], out var y) ? y : DateTime.Now.Year;
        var month = int.TryParse(parts[1], out var m) ? m : 1;

        var totalSpent = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount);

        var categories = transactions
            .Where(t => !t.IsRefund)
            .GroupBy(t => t.CategoryEntity?.Name ?? "Não Categorizado")
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Percentage = totalSpent > 0 ? Math.Round(g.Sum(t => t.Amount) / totalSpent * 100, 2) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        var date = new DateTime(year, month, 1);
        var monthName = date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

        return new InvoiceSummary
        {
            InvoiceKey = invoiceKey,
            MonthName = monthName,
            TotalSpent = totalSpent,
            TotalRefunds = transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            NetTotal = totalSpent - transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            TransactionCount = transactions.Count,
            Categories = categories
        };
    }

    public async Task<List<TransactionsByCategoryResponse>?> GetTransactionsByCategoryAsync(
        string invoiceKey,
        string? category = null)
    {
        var query = context.Transactions
            .Include(t => t.CategoryEntity)
            .Where(t => t.InvoiceKey == invoiceKey);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.CategoryEntity != null && t.CategoryEntity.Name == category);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        if (!transactions.Any())
            return null;

        return transactions
            .GroupBy(t => t.CategoryEntity != null ? t.CategoryEntity.Name : "Não Categorizado")
            .Select(g => new TransactionsByCategoryResponse
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Transactions = g.ToList()
            })
            .OrderByDescending(g => g.TotalAmount)
            .ToList();
    }

    public async Task<int> ImportTransactionsAsync(List<Transaction> transactions)
    {
        var categories = await context.Categories.ToListAsync();

        var categoryMap = categories
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var transaction in transactions)
        {
            var categoryName = string.IsNullOrWhiteSpace(transaction.Category) ? "Não Categorizado" : transaction.Category;
            if (categoryMap.TryGetValue(categoryName, out var categoryId))
            {
                transaction.CategoryId = categoryId;
            }
            else if (categoryMap.TryGetValue("Não Categorizado", out var defaultCategoryId))
            {
                transaction.CategoryId = defaultCategoryId;
            }
        }

        await EnrichInstallmentTitlesAsync(transactions);

        var invoiceKeys = transactions
            .Where(t => !string.IsNullOrEmpty(t.InvoiceKey))
            .Select(t => t.InvoiceKey!)
            .Distinct()
            .ToList();

        if (invoiceKeys.Any())
        {
            var existing = await context.Transactions
                .Where(t => invoiceKeys.Contains(t.InvoiceKey!))
                .ToListAsync();

            if (existing.Any())
                context.Transactions.RemoveRange(existing);
        }

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        return transactions.Count;
    }

    private async Task EnrichInstallmentTitlesAsync(List<Transaction> transactions)
    {
        var candidates = transactions
            .Select(t => (Transaction: t, Match: InstallmentTitleRegex.Match(t.Title ?? string.Empty)))
            .Where(x => x.Match.Success
                && !x.Match.Groups["desc"].Success
                && int.Parse(x.Match.Groups["atual"].Value) > 1)
            .Select(x => (x.Transaction, x.Match, PreviousKey: GetPreviousInvoiceKey(x.Transaction.InvoiceKey)))
            .Where(x => !string.IsNullOrEmpty(x.PreviousKey))
            .ToList();

        if (candidates.Count == 0)
            return;

        var previousKeys = candidates.Select(x => x.PreviousKey!).Distinct().ToList();

        var previousTitles = await context.Transactions
            .Where(t => previousKeys.Contains(t.InvoiceKey!))
            .Select(t => new { t.InvoiceKey, t.Title })
            .ToListAsync();

        foreach (var (transaction, match, previousKey) in candidates)
        {
            var baseTitle = match.Groups["base"].Value.Trim();
            var atual = int.Parse(match.Groups["atual"].Value);
            var total = int.Parse(match.Groups["total"].Value);
            var previousAtual = atual - 1;

            Match? previousMatch = null;
            foreach (var previous in previousTitles.Where(p => p.InvoiceKey == previousKey))
            {
                var candidateMatch = InstallmentTitleRegex.Match(previous.Title ?? string.Empty);
                if (candidateMatch.Success
                    && candidateMatch.Groups["desc"].Success
                    && int.Parse(candidateMatch.Groups["total"].Value) == total
                    && int.Parse(candidateMatch.Groups["atual"].Value) == previousAtual
                    && string.Equals(candidateMatch.Groups["base"].Value.Trim(), baseTitle, StringComparison.OrdinalIgnoreCase))
                {
                    previousMatch = candidateMatch;
                    break;
                }
            }

            if (previousMatch is null)
                continue;

            var desc = previousMatch.Groups["desc"].Value.Trim();
            transaction.Title = $"{baseTitle} - Parcela {atual}/{total} ({desc})";
        }
    }

    private static string? GetPreviousInvoiceKey(string? invoiceKey)
    {
        if (string.IsNullOrWhiteSpace(invoiceKey))
            return null;

        var parts = invoiceKey.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
            return null;

        var previous = new DateOnly(year, month, 1).AddMonths(-1);
        return $"{previous.Year}-{previous.Month:D2}";
    }
}
