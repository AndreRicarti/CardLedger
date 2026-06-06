using System.Text;
using CardLedger.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CardLedger.Tests.Services;

public sealed class CsvParserServiceTests
{
    private readonly Mock<ICategorizationService> _categorizationServiceMock = new();
    private readonly CsvParserService _sut;

    public CsvParserServiceTests()
    {
        _categorizationServiceMock
            .Setup(s => s.CategorizeTransaction(It.IsAny<string>()))
            .Returns("Alimentação");

        _sut = new CsvParserService(_categorizationServiceMock.Object);
    }

    private static Stream ToStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public async Task ParseNubankCsvAsync_ValidCsv_ReturnsTransactions()
    {
        // Arrange
        var csv = """
                  date,title,amount
                  2024-03-15,Restaurante ABC,50.00
                  2024-03-16,Supermercado XYZ,120.50
                  """;

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Restaurante ABC");
        result[0].Amount.Should().Be(50.00m);
        result[0].Date.Should().Be(new DateOnly(2024, 3, 15));
        result[0].IsRefund.Should().BeFalse();
        result[0].Source.Should().Be("nubank");
    }

    [Fact]
    public async Task ParseNubankCsvAsync_NegativeAmount_MarksAsRefund()
    {
        // Arrange
        var csv = """
                  date,title,amount
                  2024-03-15,Estorno Restaurante,-50.00
                  """;

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(1);
        result[0].IsRefund.Should().BeTrue();
        result[0].Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task ParseNubankCsvAsync_NegativeAmountWithUnquotedComma_ParsesCorrectly()
    {
        // Arrange — "- 3,99" sem aspas é dividido em dois campos pelo parser CSV
        var csv = "date,title,amount\n2026-05-30,IOF de volta de Claude.Ai Subscription,- 3,99\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(1);
        result[0].IsRefund.Should().BeTrue();
        result[0].Amount.Should().Be(3.99m);
    }

    [Fact]
    public async Task ParseNubankCsvAsync_OnlyHeader_ReturnsEmptyList()
    {
        // Arrange
        var csv = "date,title,amount\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Pagamento recebido")]
    [InlineData("Valor pendente do mês anterior")]
    public async Task ParseNubankCsvAsync_IgnoredTitle_SkipsTransaction(string title)
    {
        // Arrange
        var csv = $"date,title,amount\n2024-03-15,{title},100.00\n2024-03-16,Compra Normal,50.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Compra Normal");
    }

    [Fact]
    public async Task ParseNubankCsvAsync_InvalidLine_SkipsAndContinues()
    {
        // Arrange
        var csv = """
                  date,title,amount
                  invalid-line
                  2024-03-15,Transacao Valida,30.00
                  """;

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Transacao Valida");
    }

    [Fact]
    public async Task ParseNubankCsvAsync_TitleWithCommaInQuotes_ParsesCorrectly()
    {
        // Arrange
        var csv = "date,title,amount\n2024-03-15,\"Loja, com virgula\",75.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Loja, com virgula");
    }

    [Fact]
    public async Task ParseNubankCsvAsync_FileNameWithDate_ExtractsInvoiceKeyAsPreviousMonth()
    {
        // Arrange
        var csv = "date,title,amount\n2024-02-10,Compra,100.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv), "Nubank_2024-03-10.csv");

        // Assert
        result.Should().HaveCount(1);
        result[0].InvoiceKey.Should().Be("2024-02");
    }

    [Fact]
    public async Task ParseNubankCsvAsync_FileNameWithoutDate_InvoiceKeyIsEmpty()
    {
        // Arrange
        var csv = "date,title,amount\n2024-02-10,Compra,100.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv), "file_without_date.csv");

        // Assert
        result[0].InvoiceKey.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseNubankCsvAsync_Transaction_CallsCategorizeTransaction()
    {
        // Arrange
        var csv = "date,title,amount\n2024-03-15,Restaurante ABC,50.00\n";

        // Act
        await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        _categorizationServiceMock.Verify(
            s => s.CategorizeTransaction("Restaurante ABC"),
            Times.Once);
    }

    [Fact]
    public async Task ParseNubankCsvAsync_Transaction_SetsYearAndMonthCorrectly()
    {
        // Arrange
        var csv = "date,title,amount\n2024-07-22,Compra,10.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result[0].Year.Should().Be(2024);
        result[0].Month.Should().Be(7);
    }

    [Theory]
    [InlineData("60,00",    60.00)]   // pt-BR: comma as decimal separator (quoted in CSV)
    [InlineData("60.00",    60.00)]   // invariant: dot as decimal separator
    [InlineData("1.234,56", 1234.56)] // pt-BR: dot as thousands, comma as decimal (quoted in CSV)
    [InlineData("1,234.56", 1234.56)] // en-US: comma as thousands, dot as decimal (quoted in CSV)
    [InlineData("5,89",     5.89)]    // pt-BR without thousands separator (quoted in CSV)
    public async Task ParseNubankCsvAsync_AmountFormats_ParsedCorrectly(string amount, decimal expected)
    {
        // Amounts containing commas must be quoted in CSV to avoid being split as extra fields
        var needsQuotes = amount.Contains(',');
        var csvAmount = needsQuotes ? $"\"{amount}\"" : amount;
        var csv = $"date,title,amount\n2024-03-15,Compra,{csvAmount}\n";

        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(expected);
    }
}