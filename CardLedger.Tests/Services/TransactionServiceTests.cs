using CardLedger.Data;
using CardLedger.Models;
using CardLedger.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardLedger.Tests.Services;

public sealed class TransactionServiceTests : IDisposable
{
    private readonly InvoiceDbContext _context;
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InvoiceDbContext(options);
        _sut = new TransactionService(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task FilterTransactionsAsync_SemFiltros_RetornaTodasAsTransacoes()
    {
        // Arrange
        await SeedAsync([
            BuildTransaction(2024, 3, "Alimentação"),
            BuildTransaction(2024, 4, "Transporte"),
        ]);

        // Act
        var result = await _sut.FilterTransactionsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterTransactionsAsync_FiltroPorAno_RetornaApenasDoAno()
    {
        // Arrange
        await SeedAsync([
            BuildTransaction(2024, 3, "Alimentação"),
            BuildTransaction(2023, 3, "Alimentação"),
        ]);

        // Act
        var result = await _sut.FilterTransactionsAsync(year: 2024);

        // Assert
        result.Should().HaveCount(1);
        result[0].Year.Should().Be(2024);
    }

    [Fact]
    public async Task FilterTransactionsAsync_FiltroPorMes_RetornaApenasDoMes()
    {
        // Arrange
        await SeedAsync([
            BuildTransaction(2024, 3, "Alimentação"),
            BuildTransaction(2024, 5, "Alimentação"),
        ]);

        // Act
        var result = await _sut.FilterTransactionsAsync(month: 3);

        // Assert
        result.Should().HaveCount(1);
        result[0].Month.Should().Be(3);
    }

    [Fact]
    public async Task FilterTransactionsAsync_FiltroPorCategoria_RetornaApenasACategoria()
    {
        // Arrange
        await SeedAsync([
            BuildTransaction(2024, 3, "Alimentação"),
            BuildTransaction(2024, 3, "Transporte"),
        ]);

        // Act
        var result = await _sut.FilterTransactionsAsync(category: "Alimentação");

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Alimentação");
    }

    [Fact]
    public async Task FilterTransactionsAsync_FiltrosCombinados_RetornaApenasCombinacao()
    {
        // Arrange
        await SeedAsync([
            BuildTransaction(2024, 3, "Alimentação"),
            BuildTransaction(2024, 4, "Alimentação"),
            BuildTransaction(2023, 3, "Alimentação"),
        ]);

        // Act
        var result = await _sut.FilterTransactionsAsync(year: 2024, month: 3, category: "Alimentação");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTransactionAsync_IdExistente_RetornaTransacao()
    {
        // Arrange
        var transaction = BuildTransaction(2024, 3, "Alimentação");
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetTransactionAsync(transaction.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(transaction.Id);
    }

    [Fact]
    public async Task GetTransactionAsync_IdInexistente_RetornaNull()
    {
        // Act
        var result = await _sut.GetTransactionAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCategoryAsync_IdExistente_AtualizaCategoriaERetornaTrue()
    {
        // Arrange
        var transaction = BuildTransaction(2024, 3, "Alimentação");
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var success = await _sut.UpdateCategoryAsync(transaction.Id, "Transporte");

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Transactions.FindAsync(transaction.Id);
        updated!.Category.Should().Be("Transporte");
    }

    [Fact]
    public async Task UpdateCategoryAsync_IdInexistente_RetornaFalse()
    {
        // Act
        var success = await _sut.UpdateCategoryAsync(9999, "Transporte");

        // Assert
        success.Should().BeFalse();
    }

    private static Transaction BuildTransaction(int year, int month, string category) => new()
    {
        Title = "Transacao Teste",
        Amount = 100m,
        Category = category,
        Year = year,
        Month = month,
        Date = new DateOnly(year, month, 15),
        InvoiceKey = $"{year}-{month:D2}",
        Source = "nubank"
    };
}