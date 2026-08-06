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
        _context.Database.EnsureCreated();
        _sut = new InvoiceService(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();
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
    public async Task ImportTransactionsAsync_TransacaoDuplicada_SubstituiMesERetornaContagemImportada()
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
        count.Should().Be(1);
        _context.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ImportTransactionsAsync_ParcelaComDescricaoNoMesAnterior_HerdaDescricao()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2026-05", "Pag*Steam - Parcela 1/2 (Jogo)", 26.83m)
        ]);

        var novaParcela = BuildTransaction("2026-06", "Pag*Steam - Parcela 2/2", 26.83m);

        // Act
        await _sut.ImportTransactionsAsync([novaParcela]);

        // Assert
        var imported = await _context.Transactions.FirstAsync(t => t.InvoiceKey == "2026-06");
        imported.Title.Should().Be("Pag*Steam - Parcela 2/2 (Jogo)");
    }

    [Fact]
    public async Task ImportTransactionsAsync_ParcelaSemCorrespondenteNoMesAnterior_MantemTituloOriginal()
    {
        // Arrange
        var novaParcela = BuildTransaction("2026-06", "Pag*Steam - Parcela 2/2", 26.83m);

        // Act
        await _sut.ImportTransactionsAsync([novaParcela]);

        // Assert
        var imported = await _context.Transactions.FirstAsync(t => t.InvoiceKey == "2026-06");
        imported.Title.Should().Be("Pag*Steam - Parcela 2/2");
    }

    [Fact]
    public async Task ImportTransactionsAsync_PrimeiraParcela_NaoBuscaMesAnterior()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2026-05", "Pag*Steam - Parcela 1/2 (Jogo)", 26.83m)
        ]);

        var novaParcela = BuildTransaction("2026-06", "Pag*Steam - Parcela 1/3", 10m);

        // Act
        await _sut.ImportTransactionsAsync([novaParcela]);

        // Assert
        var imported = await _context.Transactions.FirstAsync(t => t.InvoiceKey == "2026-06");
        imported.Title.Should().Be("Pag*Steam - Parcela 1/3");
    }

    [Fact]
    public async Task ImportTransactionsAsync_TituloJaComDescricao_NaoSobrescreve()
    {
        // Arrange
        await SeedTransactionsAsync([
            BuildTransaction("2026-05", "Pag*Steam - Parcela 1/2 (Jogo)", 26.83m)
        ]);

        var novaParcela = BuildTransaction("2026-06", "Pag*Steam - Parcela 2/2 (Outro)", 26.83m);

        // Act
        await _sut.ImportTransactionsAsync([novaParcela]);

        // Assert
        var imported = await _context.Transactions.FirstAsync(t => t.InvoiceKey == "2026-06");
        imported.Title.Should().Be("Pag*Steam - Parcela 2/2 (Outro)");
    }

    private Transaction BuildTransaction(
        string invoiceKey,
        string title,
        decimal amount,
        bool isRefund = false,
        string category = "Não Categorizado")
    {
        var categoryId = _context.Categories
            .Where(c => c.Name == category)
            .Select(c => c.Id)
            .First();

        return new Transaction
        {
            InvoiceKey = invoiceKey,
            Title = title,
            Amount = amount,
            IsRefund = isRefund,
            CategoryId = categoryId,
            Date = new DateOnly(2024, 3, 15),
            Year = 2024,
            Month = 3,
            Source = "nubank"
        };
    }
}