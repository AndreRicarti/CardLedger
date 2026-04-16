using CardLedger.Data;
using CardLedger.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CardLedger.Services;

public interface IInvoiceService
{
    Task<List<MonthlyInvoice>> GetAllInvoicesAsync();
    Task<MonthlyInvoice?> GetMonthlyInvoiceAsync(int year, int month);
    Task<MonthlyInvoice?> GetInvoiceByKeyAsync(string invoiceKey);
    Task<MonthlySummary> GetMonthlySummaryAsync(int year, int month);
    Task<MonthlySummary> GetMonthlySummaryByKeyAsync(string invoiceKey);
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
            .Where(t => !string.IsNullOrEmpty(t.InvoiceKey))
            .GroupBy(t => t.InvoiceKey)
            .OrderByDescending(g => g.Key)
            .Select(g => ParseInvoiceKey(g, g.Key))
            .ToList();

        return grouped;
    }

    private MonthlyInvoice ParseInvoiceKey(IGrouping<string, Transaction> group, string invoiceKey)
    {
        var parts = invoiceKey.Split('-');
        var year = int.TryParse(parts[0], out var y) ? y : DateTime.Now.Year;
        var month = int.TryParse(parts[1], out var m) ? m : 1;

        return new MonthlyInvoice
        {
            Year = year,
            Month = month,
            InvoiceKey = invoiceKey,
            MonthName = GetMonthName(year, month),
            TotalSpent = group.Where(t => !t.IsRefund).Sum(t => t.Amount),
            TotalRefunds = group.Where(t => t.IsRefund).Sum(t => t.Amount),
            NetTotal = group.Where(t => !t.IsRefund).Sum(t => t.Amount) - group.Where(t => t.IsRefund).Sum(t => t.Amount),
            TransactionCount = group.Count()
        };
    }

    public async Task<MonthlyInvoice?> GetMonthlyInvoiceAsync(int year, int month)
    {
        var transactions = await _context.Transactions
            .Where(t => t.Year == year && t.Month == month)
            .ToListAsync();

        if (!transactions.Any())
            return null;

        var invoiceKey = transactions.FirstOrDefault()?.InvoiceKey ?? $"{year}-{month:D2}";

        return new MonthlyInvoice
        {
            Year = year,
            Month = month,
            InvoiceKey = invoiceKey,
            MonthName = GetMonthName(year, month),
            TotalSpent = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount),
            TotalRefunds = transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            NetTotal = transactions.Where(t => !t.IsRefund).Sum(t => t.Amount) - transactions.Where(t => t.IsRefund).Sum(t => t.Amount),
            TransactionCount = transactions.Count
        };
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

    public async Task<MonthlySummary> GetMonthlySummaryByKeyAsync(string invoiceKey)
    {
        var transactions = await _context.Transactions
            .Where(t => t.InvoiceKey == invoiceKey && !t.IsRefund)
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
