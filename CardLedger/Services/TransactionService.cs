using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Services
{
    public interface ITransactionService
    {
        Task<List<Transaction>> FilterTransactionsAsync(int? year = null, int? month = null, string? category = null);
        Task<Transaction?> GetTransactionAsync(int id);
        Task<bool> UpdateCategoryAsync(int id, string category);
        Task<bool> UpdateCategoryByInvoiceAsync(string invoiceKey, int id, string category);
        Task<List<string>> GetCategoriesAsync();
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
            var query = _context.Transactions.AsQueryable();

            if (year.HasValue)
                query = query.Where(t => t.Year == year);

            if (month.HasValue)
                query = query.Where(t => t.Month == month);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            return await query.OrderByDescending(t => t.Date).ToListAsync();
        }

        public async Task<Transaction?> GetTransactionAsync(int id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<bool> UpdateCategoryAsync(int id, string category)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return false;

            transaction.Category = category;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryByInvoiceAsync(string invoiceKey, int id, string category)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.InvoiceKey == invoiceKey);

            if (transaction == null)
                return false;

            transaction.Category = category;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Transactions
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
