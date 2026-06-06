using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Services;

public interface IInvoiceService
{
    Task<List<TransactionsByCategoryResponse>?> GetTransactionsByCategoryAsync(string invoiceKey, string? category = null);
    Task<int> ImportTransactionsAsync(List<Transaction> transactions);
}

public sealed class InvoiceService : IInvoiceService
{
    private readonly InvoiceDbContext _context;

    public InvoiceService(InvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionsByCategoryResponse>?> GetTransactionsByCategoryAsync(
        string invoiceKey,
        string? category = null)
    {
        var query = _context.Transactions
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
        var categories = await _context.Categories.ToListAsync();
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

        var invoiceKeys = transactions
            .Where(t => !string.IsNullOrEmpty(t.InvoiceKey))
            .Select(t => t.InvoiceKey!)
            .Distinct()
            .ToList();

        if (invoiceKeys.Any())
        {
            var existing = await _context.Transactions
                .Where(t => invoiceKeys.Contains(t.InvoiceKey!))
                .ToListAsync();

            if (existing.Any())
                _context.Transactions.RemoveRange(existing);
        }

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        return transactions.Count;
    }

}
