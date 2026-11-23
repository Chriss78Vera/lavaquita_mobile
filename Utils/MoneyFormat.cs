using System.Globalization;

namespace LaVaquita.Utils;

public static class MoneyFormat
{
    public static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");

    public static string Format(decimal amount, string? currency = "USD")
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        return $"{amount.ToString("N2", UnitedStates)} {code}";
    }

    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = text.Trim().Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, UnitedStates, out amount);
    }
}
