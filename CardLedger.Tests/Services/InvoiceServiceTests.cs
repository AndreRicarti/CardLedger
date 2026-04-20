using CardLedger.Data;
using CardLedger.Models;
using CardLedger.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardLedger.Tests.Services;

public sealed class InvoiceServiceTests : IDisposable
{
    private readonly InvoiceDbContext _context;
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InvoiceDbContext(options);
        _sut = new InvoiceService(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetInvoiceByKeyAsync_ChaveExistente_RetornaMonthlyInvoice()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2024-03", "Restaurante", 100m, isRefund: false),
            BuildTransaction("2024-03", "Supermercado", 200m, isRefund: false),
        ]);

        // Act
        var result = await _sut.GetInvoiceByKeyAsync("2024-03");

        // Assert
        result.Should().NotBeNull();
        result!.InvoiceKey.Should().Be("2024-03");
        result.TotalSpent.Should().Be(300m);
        result.TotalRefunds.Should().Be(0m);
        result.NetTotal.Should().Be(300m);
        result.TransactionCount.Should().Be(2);
        result.Year.Should().Be(2024);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task GetInvoiceByKeyAsync_ComEstorno_CalculaTotaisCorretamente()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2024-03", "Compra", 200m, isRefund: false),
            BuildTransaction("2024-03", "Estorno Compra", 50m, isRefund: true),
        ]);

        // Act
        var result = await _sut.GetInvoiceByKeyAsync("2024-03");

        // Assert
        result!.TotalSpent.Should().Be(200m);
        result.TotalRefunds.Should().Be(50m);
        result.NetTotal.Should().Be(150m);
    }

    [Fact]
    public async Task GetInvoiceByKeyAsync_ChaveInexistente_RetornaNull()
    {
        // Act
        var result = await _sut.GetInvoiceByKeyAsync("2099-01");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoiceSummaryByKeyAsync_ChaveExistente_RetornaResumoComCategorias()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2024-03", "Restaurante", 100m, category: "Alimentação"),
            BuildTransaction("2024-03", "Uber", 50m, category: "Transporte"),
        ]);

        // Act
        var result = await _sut.GetInvoiceSummaryByKeyAsync("2024-03");

        // Assert
        result.Should().NotBeNull();
        result!.TotalSpent.Should().Be(150m);
        result.Categories.Should().HaveCount(2);
        result.Categories[0].Category.Should().Be("Alimentação");
        result.Categories[0].Amount.Should().Be(100m);
        result.Categories[0].Percentage.Should().BeApproximately(66.67m, 0.01m);
    }

    [Fact]
    public async Task GetInvoiceSummaryByKeyAsync_ChaveInexistente_RetornaNull()
    {
        // Act
        var result = await _sut.GetInvoiceSummaryByKeyAsync("2099-01");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTransactionsByCategoryAsync_ChaveExistente_AgrupaPorCategoria()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2024-03", "Restaurante A", 80m, category: "Alimentação"),
            BuildTransaction("2024-03", "Restaurante B", 60m, category: "Alimentação"),
            BuildTransaction("2024-03", "Uber", 30m, category: "Transporte"),
        ]);

        // Act
        var result = await _sut.GetTransactionsByCategoryAsync("2024-03");

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result[0].Category.Should().Be("Alimentação");
        result[0].TotalAmount.Should().Be(140m);
        result[0].TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTransactionsByCategoryAsync_ChaveInexistente_RetornaNull()
    {
        // Act
        var result = await _sut.GetTransactionsByCategoryAsync("2099-01");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ImportTransactionsAsync_TransacoesNovas_InsereERetornaContagem()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            BuildTransaction("2024-03", "Compra A", 100m),
            BuildTransaction("2024-03", "Compra B", 200m),
        };

        // Act
        var count = await _sut.ImportTransactionsAsync(transactions);

        // Assert
        count.Should().Be(2);
        _context.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportTransactionsAsync_TransacaoDuplicada_NaoInsereNovamente()
    {
        // Arrange
        var existing = BuildTransaction("2024-03", "Compra A", 100m);
        existing.Date = new DateOnly(2024, 3, 10);
        await SeedTransactionsAsync([existing]);

        var duplicate = BuildTransaction("2024-03", "Compra A", 100m);
        duplicate.Date = new DateOnly(2024, 3, 10);

        // Act
        var count = await _sut.ImportTransactionsAsync([duplicate]);

        // Assert
        count.Should().Be(0);
        _context.Transactions.Should().HaveCount(1);
    }

    private static Transaction BuildTransaction(
        string invoiceKey,
        string title,
        decimal amount,
        bool isRefund = false,
        string category = "Não Categorizado") => new()
    {
        InvoiceKey = invoiceKey,
        Title = title,
        Amount = amount,
        IsRefund = isRefund,
        Category = category,
        Date = new DateOnly(2024, 3, 15),
        Year = 2024,
        Month = 3,
        Source = "nubank"
    };
}