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
        _context.Database.EnsureCreated();
        _sut = new TransactionService(_context);
    }

    public void Dispose() => _context.Dispose();

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

        var transporteId = await _context.Categories
            .Where(c => c.Name == "Transporte")
            .Select(c => c.Id)
            .FirstAsync();

        // Act
        var success = await _sut.UpdateCategoryAsync(transaction.Id, transporteId);

        // Assert
        success.Should().BeTrue();
        var updated = await _context.Transactions
            .Include(t => t.CategoryEntity)
            .FirstAsync(t => t.Id == transaction.Id);
        updated.CategoryEntity!.Name.Should().Be("Transporte");
    }

    [Fact]
    public async Task UpdateCategoryAsync_IdInexistente_RetornaFalse()
    {
        var transporteId = await _context.Categories
            .Where(c => c.Name == "Transporte")
            .Select(c => c.Id)
            .FirstAsync();

        // Act
        var success = await _sut.UpdateCategoryAsync(9999, transporteId);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategoriesAsync_RetornaIdENomeOrdenado()
    {
        // Act
        var result = await _sut.GetCategoriesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(c => c.Id > 0 && !string.IsNullOrWhiteSpace(c.Name));
        result.Select(c => c.Name).Should().BeInAscendingOrder();
        result.Should().Contain(c => c.Name == "Compras Avulsas");
        result.Should().Contain(c => c.Name == "Terceiros");
    }

    [Fact]
    public async Task UpdateCategoryAsync_CategoryIdInvalido_RetornaFalse()
    {
        // Arrange
        var transaction = BuildTransaction(2024, 3, "Alimentação");
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var success = await _sut.UpdateCategoryAsync(transaction.Id, 999999);

        // Assert
        success.Should().BeFalse();
    }

    private Transaction BuildTransaction(int year, int month, string category)
    {
        var categoryId = _context.Categories
            .Where(c => c.Name == category)
            .Select(c => c.Id)
            .First();

        return new Transaction
        {
            Title = "Transacao Teste",
            Amount = 100m,
            CategoryId = categoryId,
            Year = year,
            Month = month,
            Date = new DateOnly(year, month, 15),
            InvoiceKey = $"{year}-{month:D2}",
            Source = "nubank"
        };
    }
}