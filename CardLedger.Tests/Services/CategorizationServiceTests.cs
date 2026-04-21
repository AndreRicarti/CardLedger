using CardLedger.Services;
using FluentAssertions;
using Xunit;

namespace CardLedger.Tests.Services;

public sealed class CategorizationServiceTests
{
    private readonly CategorizationService _sut = new();

    [Theory]
    [InlineData("restaurante do bairro", "Alimentação")]
    [InlineData("pizzaria napoli", "Alimentação")]
    [InlineData("supermercado extra", "Alimentação")]
    [InlineData("padaria central", "Alimentação")]
    [InlineData("uber trip", "Transporte")]
    [InlineData("99 corrida", "Transporte")]
    [InlineData("posto de gasolina", "Transporte")]
    [InlineData("youtube premium", "Assinaturas & Contas")]
    [InlineData("Conta Vivo", "Assinaturas & Contas")]
    [InlineData("Ig*Floraenergia", "Assinaturas & Contas")]
    [InlineData("netflix", "Assinaturas & Contas")]
    [InlineData("spotify", "Assinaturas & Contas")]
    [InlineData("shopee compra", "Compras Online")]
    [InlineData("amazon pedido", "Compras Online")]
    [InlineData("farmacia popular", "Saúde")]
    [InlineData("hospital das clinicas", "Saúde")]
    [InlineData("udemy curso", "Educação")]
    [InlineData("conta de energia", "Utilidades")]
    public void CategorizeTransaction_TituloConhecido_RetornaCategoriaCerta(string title, string expectedCategory)
    {
        // Act
        var result = _sut.CategorizeTransaction(title);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void CategorizeTransaction_TituloVazioOuNulo_RetornaNaoCategorizado(string? title)
    {
        // Act
        var result = _sut.CategorizeTransaction(title!);

        // Assert
        result.Should().Be("Não Categorizado");
    }

    [Fact]
    public void CategorizeTransaction_TituloSemCorrespondencia_RetornaNaoCategorizado()
    {
        // Act
        var result = _sut.CategorizeTransaction("xyzabc pagamento generico 12345");

        // Assert
        result.Should().Be("Não Categorizado");
    }

    [Theory]
    [InlineData("KaBuM! - NuPay - Parcela 1/5")]
    [InlineData("Loja ABC Parcela 2/12")]
    [InlineData("parcela 3/6 produto")]
    public void CategorizeTransaction_TituloComParcela_RetornaParcelado(string title)
    {
        // Act
        var result = _sut.CategorizeTransaction(title);

        // Assert
        result.Should().Be("Parcelado");
    }
}