using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Services
{
    public interface ITransactionService
    {
        Task<List<Transaction>> FilterTransactionsAsync(int? year = null, int? month = null, string? category = null);
        Task<Transaction?> GetTransactionAsync(int id);
        Task<bool> UpdateCategoryAsync(int id, int categoryId);
        Task<bool> UpdateCategoryByInvoiceAsync(string invoiceKey, int id, int categoryId);
        Task<List<CategoryOption>> GetCategoriesAsync();
    }

    public sealed class TransactionService : ITransactionService
    {
        private readonly InvoiceDbContext _context;

        public TransactionService(InvoiceDbContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> FilterTransactionsAsync(int? year = null, int? month = null, string? category = null)
        {
            var query = _context.Transactions
                .Include(t => t.CategoryEntity)
                .AsQueryable();

            if (year.HasValue)
                query = query.Where(t => t.Year == year);

            if (month.HasValue)
                query = query.Where(t => t.Month == month);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.CategoryEntity != null && t.CategoryEntity.Name == category);

            return await query.OrderByDescending(t => t.Date).ToListAsync();
        }

        public async Task<Transaction?> GetTransactionAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.CategoryEntity)
                .FirstOrDefaultAsync(t => t.Id == id);
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

        public async Task<bool> UpdateCategoryByInvoiceAsync(string invoiceKey, int id, int categoryId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.InvoiceKey == invoiceKey);

            if (transaction == null)
                return false;

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
            if (!categoryExists)
                return false;

            transaction.CategoryId = categoryId;
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
}
