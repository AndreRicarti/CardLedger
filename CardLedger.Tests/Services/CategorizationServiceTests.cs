using CardLedger.Services;
using FluentAssertions;
using Xunit;

namespace CardLedger.Tests.Services;

public sealed class CategorizationServiceTests
{
    private readonly CategorizationService _sut = new();

    [Theory]
    [InlineData("restaurante do bairro", "Alimentação")]
    [InlineData("55.769.239 Ana Paula R", "Alimentação")]
    [InlineData("Jw Lima", "Alimentação")]
    [InlineData("pizzaria napoli", "Alimentação")]
    [InlineData("pastel da feira", "Alimentação")]
    [InlineData("supermercado extra", "Supermercado")]
    [InlineData("padaria central", "Alimentação")]
    [InlineData("uber trip", "Transporte")]
    [InlineData("UberRides*Trip", "Transporte")]
    [InlineData("99 corrida", "Transporte")]
    [InlineData("Github Pro", "Assinaturas & Contas")]
    [InlineData("compra avulsa marketplace", "Compras Avulsas")]
    [InlineData("pagamento terceiros", "Terceiros")]
    [InlineData("posto de gasolina", "Transporte")]
    [InlineData("youtube premium", "Assinaturas & Contas")]
    [InlineData("Conta Vivo", "Assinaturas & Contas")]
    [InlineData("Ig*Floraenergia", "Assinaturas & Contas")]
    [InlineData("netflix", "Assinaturas & Contas")]
    [InlineData("spotify", "Assinaturas & Contas")]
    [InlineData("shopee compra", "Mayara")]
    [InlineData("amazon pedido", "Compras Online")]
    [InlineData("farmacia popular", "Saúde")]
    [InlineData("Rdsaude Online", "Saúde")]
    [InlineData("hospital das clinicas", "Saúde")]
    [InlineData("udemy curso", "Educação")]
    [InlineData("conta de energia", "Utilidades")]
    // Assinaturas & Contas - contas fixas
    [InlineData("Enel Distribuicao", "Assinaturas & Contas")]
    [InlineData("Flexpag*Enelsp", "Assinaturas & Contas")]
    // Carro
    [InlineData("Park Car One", "Carro")]
    [InlineData("Zona Azul Barueri", "Carro")]
    [InlineData("NuTag*EIQ0E15", "Carro")]
    [InlineData("Ec *Shellbox", "Carro")]
    [InlineData("Rei Tupa", "Carro")]
    // Games
    [InlineData("Steam Purchase", "Games")]
    [InlineData("Greenman Gaming", "Games")]
    [InlineData("Fmu Mensalidade", "Assinaturas & Contas")]
    [InlineData("Sabesp Fatura", "Assinaturas & Contas")]
    [InlineData("Melimais Assinatura", "Assinaturas & Contas")]
    [InlineData("Alares Internet", "Assinaturas & Contas")]
    [InlineData("Youtubepremium", "Assinaturas & Contas")]
    // Supermercado
    [InlineData("Supermercado Estrela", "Supermercado")]
    [InlineData("Atacadao 655 As", "Supermercado")]
    [InlineData("Quasetudo", "Supermercado")]
    [InlineData("Carrefour Tbe", "Supermercado")]
    [InlineData("Ifd*Supermercados Irma", "Supermercado")]
    [InlineData("Nescafe Dolce Gusto", "Supermercado")]
    [InlineData("Center Car Bom Corte", "Supermercado")]
    // Alimentação
    [InlineData("Thoca Beatriz", "Alimentação")]
    [InlineData("Keeta Delivery", "Alimentação")]
    [InlineData("Ragazzo", "Alimentação")]
    [InlineData("Restaur 123", "Alimentação")]
    [InlineData("Salgados do Ze", "Alimentação")]
    [InlineData("Thoca Burguer", "Alimentação")]
    [InlineData("Mcdonaldsecommerce", "Alimentação")]
    [InlineData("99food *32.106.540 Gio", "Alimentação")]
    [InlineData("Barueri Drive", "Alimentação")]
    [InlineData("Emporio Rei do Norte", "Alimentação")]
    [InlineData("Ifd*Nb Point Acai", "Alimentação")]
    [InlineData("Ifd*Pastel do Vini", "Alimentação")]
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