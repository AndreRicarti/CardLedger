using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CardLedger.Services;

public interface IInvoiceService
{
    Task<List<MonthlyInvoice>> GetAllInvoicesAsync();
    Task<MonthlyInvoice?> GetMonthlyInvoiceAsync(int year, int month);
    Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month);
    Task<int> ImportTransactionsAsync(List<Transaction> transactions);
}

public sealed class InvoiceService : IInvoiceService
{
    private readonly InvoiceDbContext _context;

    public InvoiceService(InvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<List<MonthlyInvoice>> GetAllInvoicesAsync()
    {
        var transactions = await _context.Transactions.ToListAsync();

        var grouped = transactions
            .GroupBy(t => new { t.Year, t.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new MonthlyInvoice
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = GetMonthName(g.Key.Year, g.Key.Month),
                TotalSpent = g.Where(t => !t.IsRefund).Sum(t => t.Amount),
                TotalRefunds = g.Where(t => t.IsRefund).Sum(t => t.Amount),
                NetTotal = g.Where(t => !t.IsRefund).Sum(t => t.Amount) - g.Where(t => t.IsRefund).Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .ToList();

        return grouped;
    }

    public async Task<MonthlyInvoice?> GetMonthlyInvoiceAsync(int year, int month)
    {
        var transactions = await _context.Transactions
            .Where(t => t.Year == year && t.Month == month)
            .ToListAsync();

        if (!transactions.Any())
            return null;

        return new MonthlyInvoice
        {
            Year = year,
            Month = month,
            MonthName = GetMonthName(year, month),
            TotalSpent = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount),
            TotalRefunds = transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            NetTotal = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount) - transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            TransactionCount = transactions.Count
        };
    }

    public async Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month)
    {
        var transactions = await _context.Transactions
            .Where(t => t.Year == year && t.Month == month && !t.IsRefund)
            .ToListAsync();

        var total = transactions.Sum(t => t.Amount);

        var byCategory = transactions
            .GroupBy(t => t.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Percentage = total > 0 ? decimal.Round((g.Sum(t => t.Amount) / total) * 100, 1) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new MonthlySummary
        {
            Total = total,
            ByCategory = byCategory
        };
    }

    public async Task<int> ImportTransactionsAsync(List<Transaction> transactions)
    {
        // Evita duplicatas
        var existingDates = await _context.Transactions
            .Select(t => new { t.Date, t.Title, t.Amount })
            .ToListAsync();

        var newTransactions = transactions
            .Where(t => !existingDates.Any(e => e.Date == t.Date && e.Title == t.Title && e.Amount == t.Amount))
            .ToList();

        if (newTransactions.Any())
        {
            _context.Transactions.AddRange(newTransactions);
            await _context.SaveChangesAsync();
        }

        return newTransactions.Count;
    }

    private string GetMonthName(int year, int month)
    {
        var date = new DateTime(year, month, 1);
        var culture = new CultureInfo("pt-BR");
        return date.ToString("MMMM yyyy", culture);
    }
}
