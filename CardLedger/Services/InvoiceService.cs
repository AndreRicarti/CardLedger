using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CardLedger.Services;

public interface IInvoiceService
{
    Task<MonthlyInvoice?> GetInvoiceByKeyAsync(string invoiceKey);
    Task<InvoiceSummary?> GetInvoiceSummaryByKeyAsync(string invoiceKey);
    Task<int> ImportTransactionsAsync(List<Transaction> transactions);
}

public sealed class InvoiceService : IInvoiceService
{
    private readonly InvoiceDbContext _context;

    public InvoiceService(InvoiceDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlyInvoice?> GetInvoiceByKeyAsync(string invoiceKey)
    {
        var transactions = await _context.Transactions
            .Where(t => t.InvoiceKey == invoiceKey)
            .ToListAsync();

        if (!transactions.Any())
            return null;

        var parts = invoiceKey.Split('-');
        var year = int.TryParse(parts[0], out var y) ? y : DateTime.Now.Year;
        var month = int.TryParse(parts[1], out var m) ? m : 1;

        return new MonthlyInvoice
        {
            Year = year,
            Month = month,
            InvoiceKey = invoiceKey,
            MonthName = GetMonthName(year, month),
            TotalSpent = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount),
            TotalRefunds = transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            NetTotal = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount) - transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            TransactionCount = transactions.Count()
        };
    }

    public async Task<InvoiceSummary?> GetInvoiceSummaryByKeyAsync(string invoiceKey)
    {
        var transactions = await _context.Transactions
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
            .GroupBy(t => t.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Percentage = totalSpent > 0 ? Math.Round(g.Sum(t => t.Amount) / totalSpent * 100, 2) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new InvoiceSummary
        {
            InvoiceKey = invoiceKey,
            MonthName = GetMonthName(year, month),
            TotalSpent = totalSpent,
            TotalRefunds = transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            NetTotal = totalSpent - transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            TransactionCount = transactions.Count,
            Categories = categories
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
