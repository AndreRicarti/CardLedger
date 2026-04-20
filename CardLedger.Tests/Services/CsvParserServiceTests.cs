using CardLedger.Services;
using FluentAssertions;
using Moq;
using System.Text;
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

    private static Stream ToStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ParseNubankCsvAsync_CsvValido_RetornaTransacoes()
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
    public async Task ParseNubankCsvAsync_ValorNegativo_MarcaComoEstorno()
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
    public async Task ParseNubankCsvAsync_CsvApenasCabecalho_RetornaListaVazia()
    {
        // Arrange
        var csv = "date,title,amount\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseNubankCsvAsync_LinhaInvalida_IgnoraEContinua()
    {
        // Arrange
        var csv = """
            date,title,amount
            linha-invalida
            2024-03-15,Transacao Valida,30.00
            """;

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Transacao Valida");
    }

    [Fact]
    public async Task ParseNubankCsvAsync_TituloComVirgulaNasAspas_ParseCorretamente()
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
    public async Task ParseNubankCsvAsync_NomeArquivoComData_ExtractInvoiceKeyMesAnterior()
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
    public async Task ParseNubankCsvAsync_NomeArquivoSemData_InvoiceKeyVazia()
    {
        // Arrange
        var csv = "date,title,amount\n2024-02-10,Compra,100.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv), "arquivo_sem_data.csv");

        // Assert
        result[0].InvoiceKey.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseNubankCsvAsync_Transacao_ChamaCategorizeTransaction()
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
    public async Task ParseNubankCsvAsync_Transacao_PreencheAnoEMesCorretamente()
    {
        // Arrange
        var csv = "date,title,amount\n2024-07-22,Compra,10.00\n";

        // Act
        var result = await _sut.ParseNubankCsvAsync(ToStream(csv));

        // Assert
        result[0].Year.Should().Be(2024);
        result[0].Month.Should().Be(7);
    }
}