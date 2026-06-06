using System.Globalization;

namespace CardLedger.Helpers;

public static class DecimalParser
{
    /// <summary>
    /// Parseia valores decimais nos formatos pt-BR e en-US.
    /// Suporta: "60,00" → 60, "60.00" → 60, "1.234,56" → 1234.56, "1,234.56" → 1234.56
    /// </summary>
    public static bool TryParseAmount(string value, out decimal result)
    {
        var normalized = value.Trim();

        var lastDot   = normalized.LastIndexOf('.');
        var lastComma = normalized.LastIndexOf(',');

        if (lastDot >= 0 && lastComma >= 0)
        {
            normalized = lastComma > lastDot
                ? normalized.Replace(".", "").Replace(",", ".")   // 1.234,56 → 1234.56
                : normalized.Replace(",", "");                    // 1,234.56 → 1234.56
        }
        else if (lastComma >= 0)
        {
            normalized = normalized.Replace(",", ".");            // 60,00 → 60.00
        }

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}
