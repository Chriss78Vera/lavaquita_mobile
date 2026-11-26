using System.Globalization;
using System.Text.RegularExpressions;

namespace LaVaquita.Utils;

public static class MoneyFormat
{
    public static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");
    public const string MensajeMontoInvalido = "Ingresa un monto mayor que cero, de hasta 21.474.836,47 USD y con máximo dos decimales.";
    private const decimal MontoMaximo = int.MaxValue / 100m;
    private static readonly Regex FormatoEntrada = new(@"\A[0-9]{1,8}(?:[.,][0-9]{0,2})?\z");

    public static string Format(decimal amount, string? currency = "USD")
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        return $"{amount.ToString("N2", UnitedStates)} {code}";
    }

    public static bool EsEntradaValida(string? text) =>
        string.IsNullOrEmpty(text) || FormatoEntrada.IsMatch(text);

    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrEmpty(text) || !FormatoEntrada.IsMatch(text) || text[^1] is '.' or ',')
            return false;

        return decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, UnitedStates, out amount)
            && amount > 0 && amount <= MontoMaximo;
    }
}
