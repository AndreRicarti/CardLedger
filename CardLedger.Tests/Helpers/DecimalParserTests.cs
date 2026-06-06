using CardLedger.Helpers;
using FluentAssertions;
using Xunit;

namespace CardLedger.Tests.Helpers;

public sealed class DecimalParserTests
{
    [Theory]
    [InlineData("60,00",     60.00)]
    [InlineData("60.00",     60.00)]
    [InlineData("5,89",       5.89)]
    [InlineData("5.89",       5.89)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("1000",    1000.00)]
    [InlineData("0,99",       0.99)]
    [InlineData("-60,00",    -60.00)]
    [InlineData("-60.00",    -60.00)]
    [InlineData("- 3,99",    -3.99)]
    [InlineData("- 60,00",  -60.00)]
    public void TryParseAmount_FormatosValidos_RetornaValorCorreto(string input, decimal expected)
    {
        var success = DecimalParser.TryParseAmount(input, out var result);

        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("60,00,00")]
    public void TryParseAmount_FormatosInvalidos_RetornaFalse(string input)
    {
        var success = DecimalParser.TryParseAmount(input, out var result);

        success.Should().BeFalse();
        result.Should().Be(0);
    }
}
