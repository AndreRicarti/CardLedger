using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Services;

public interface ITransactionService
{
    Task<bool> UpdateCategoryAsync(int id, int categoryId);
    Task<bool> UpdateTitleAsync(string invoiceKey, int id, string title);
    Task<bool> DeleteByInvoiceKeyAsync(string invoiceKey);
    Task<List<CategoryOption>> GetCategoriesAsync();
}

public sealed class TransactionService : ITransactionService
{
    private readonly InvoiceDbContext _context;

    public TransactionService(InvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> UpdateCategoryAsync(int id, int categoryId)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
            return false;

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
            return false;

        transaction.CategoryId = categoryId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateTitleAsync(string invoiceKey, int id, string title)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.InvoiceKey == invoiceKey);

        if (transaction == null)
            return false;

        transaction.Title = title;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByInvoiceKeyAsync(string invoiceKey)
    {
        var transactions = await _context.Transactions
            .Where(t => t.InvoiceKey == invoiceKey)
            .ToListAsync();

        if (transactions.Count == 0)
            return false;

        _context.Transactions.RemoveRange(transactions);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CategoryOption>> GetCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}
