using System.Text.RegularExpressions;

namespace LaVaquita.Utils;

public static partial class CodigoSalaParser
{
    [GeneratedRegex(@"(?i)(?<code>[A-Z]{4}-[0-9]{2})")]
    private static partial Regex CodigoRegex();

    public static bool TryParse(string? contenido, out string codigo)
    {
        var match = CodigoRegex().Match(contenido?.Trim() ?? string.Empty);
        codigo = match.Success ? match.Groups["code"].Value.ToUpperInvariant() : string.Empty;
        return match.Success;
    }
}
