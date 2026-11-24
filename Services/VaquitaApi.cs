using System.Net.Http.Json;
using System.Text.Json;
using LaVaquita.Models;

namespace LaVaquita.Services;

public sealed class VaquitaApi(HttpClient httpClient)
{
    private readonly HttpClient _http = httpClient;

    public string ObtenerUrlQr(string codigo) =>
        $"https://quickchart.io/qr?size=320&margin=2&text={Uri.EscapeDataString(codigo)}";

    public async Task<CredencialesSala> CrearSalaAsync(string nombre, string avatar)
    {
        using var response = await _http.PostAsJsonAsync("api/rooms", new { hostName = nombre, hostAvatar = avatar });
        var json = await LeerAsync(response);
        return new CredencialesSala
        {
            Codigo = json.GetProperty("room").GetProperty("code").GetString()!,
            HostToken = json.GetProperty("hostToken").GetString()!,
            ParticipantToken = json.GetProperty("participantToken").GetString()!
        };
    }

    public async Task<string> UnirseAsync(string codigo, string nombre, string avatar)
    {
        using var response = await _http.PostAsJsonAsync($"api/rooms/{codigo}/participants", new { name = nombre, avatar });
        var json = await LeerAsync(response);
        return json.GetProperty("participantToken").GetString()!;
    }

    public async Task SalirSalaAsync(string codigo, string participantToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/rooms/{codigo}/participants/me");
        request.Headers.Add("x-participant-token", participantToken);
        using var response = await _http.SendAsync(request);
        await LeerAsync(response);
    }

    public async Task<Sala> ObtenerSalaAsync(string codigo, CancellationToken token = default)
    {
        using var response = await _http.GetAsync($"api/rooms/{codigo}", token);
        var json = await LeerAsync(response, token);
        var sala = new Sala
        {
            Codigo = codigo,
            Estado = json.TryGetProperty("status", out var status) ? status.GetString() ?? "waiting" : "waiting",
            FechaExpiracion = json.TryGetProperty("expiresAt", out var expires) && DateTime.TryParse(expires.GetString(), out var date) ? date : DateTime.UtcNow,
            Cuenta = new Cuenta
            {
                Total = json.TryGetProperty("amount", out var amount) && amount.ValueKind == JsonValueKind.Number ? amount.GetDecimal() : 0,
                Moneda = json.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "USD" : "USD"
            }
        };
        sala.Participantes = json.GetProperty("participants").EnumerateArray().Select(item => new Participante
        {
            Id = item.GetProperty("id").GetString()!,
            Nombre = item.GetProperty("name").GetString()!,
            Avatar = item.TryGetProperty("avatar", out var avatar) ? avatar.GetString() ?? "sparkles" : "sparkles",
            EsAnfitrion = item.TryGetProperty("isHost", out var host) && host.GetBoolean()
        }).ToList();
        sala.UltimoEvento = json.TryGetProperty("latestEvent", out var game) && game.ValueKind != JsonValueKind.Null ? game.Clone() : null;
        return sala;
    }

    public async Task ConfirmarCuentaAsync(string codigo, string hostToken, Cuenta cuenta)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/rooms/{codigo}/bill")
        { Content = JsonContent.Create(new { amount = cuenta.Total, currency = cuenta.Moneda }) };
        request.Headers.Add("x-host-token", hostToken);
        using var response = await _http.SendAsync(request);
        await LeerAsync(response);
    }

    public async Task CerrarSalaAsync(string codigo, string hostToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/rooms/{codigo}/close");
        request.Headers.Add("x-host-token", hostToken);
        using var response = await _http.SendAsync(request);
        await LeerAsync(response);
    }

    public async Task<ResultadoJuego> DividirAsync(string codigo, string? hostToken = null)
    {
        if (!string.IsNullOrWhiteSpace(hostToken))
            return ConvertirEvento(await EnviarJuegoAsync(codigo, hostToken, "split", new { }));
        using var response = await _http.GetAsync($"api/rooms/{codigo}/split");
        var json = await LeerAsync(response);
        return new ResultadoJuego
        {
            Tipo = TipoResultado.Division,
            Titulo = "A cada uno le toca",
            Subtitulo = "Los centavos coinciden con el total",
            Detalles = json.GetProperty("shares").EnumerateArray().Select(item => new DetallePago
            {
                ParticipanteId = item.GetProperty("participantId").GetString()!,
                Nombre = item.GetProperty("name").GetString()!,
                Monto = item.GetProperty("amount").GetDecimal()
            }).ToList()
        };
    }

    public async Task<ResultadoJuego> RuletaAsync(string codigo, string hostToken, int cantidad) =>
        ConvertirEvento(await EnviarJuegoAsync(codigo, hostToken, "roulette", new { payerCount = cantidad }));

    public async Task<ResultadoJuego> DadosAsync(string codigo, string hostToken, IEnumerable<string>? participantes = null)
    {
        var ids = participantes?.ToArray();
        object body = ids is { Length: >= 2 } ? new { participantIds = ids } : new { };
        return ConvertirEvento(await EnviarJuegoAsync(codigo, hostToken, "dice", body));
    }

    public ResultadoJuego ConvertirEvento(JsonElement game)
    {
        var type = game.TryGetProperty("type", out var typeNode) ? typeNode.GetString() : null;
        var payload = game.TryGetProperty("payload", out var payloadNode) ? payloadNode : game;
        if (type == "split")
        {
            return new ResultadoJuego
            {
                Tipo = TipoResultado.Division, Titulo = "A cada uno le toca", Subtitulo = "Los centavos coinciden con el total",
                Detalles = payload.GetProperty("shares").EnumerateArray().Select(item => new DetallePago
                {
                    ParticipanteId = item.GetProperty("participantId").GetString()!,
                    Nombre = item.GetProperty("name").GetString()!,
                    Monto = item.GetProperty("amount").GetDecimal()
                }).ToList()
            };
        }
        if (type == "roulette")
        {
            var shares = payload.GetProperty("shares").EnumerateArray().ToList();
            return new ResultadoJuego
            {
                Tipo = TipoResultado.Ruleta, Titulo = "La suerte habló", Subtitulo = "Ellos pagan esta vez",
                Detalles = payload.GetProperty("payers").EnumerateArray().Select(payer =>
                {
                    var id = payer.GetProperty("id").GetString()!;
                    var share = shares.First(item => item.GetProperty("participantId").GetString() == id);
                    return new DetallePago { ParticipanteId = id, Nombre = payer.GetProperty("name").GetString()!, Monto = share.GetProperty("amount").GetDecimal() };
                }).ToList()
            };
        }
        var results = payload.GetProperty("results").EnumerateArray().ToList();
        var highest = results.Max(item => item.GetProperty("total").GetInt32());
        if (type == "dice_tie")
        {
            var tiedIds = payload.GetProperty("tiedParticipantIds").EnumerateArray().Select(item => item.GetString()).ToHashSet();
            return new ResultadoJuego
            {
                Tipo = TipoResultado.Empate, Titulo = "¡Tenemos empate!", Subtitulo = "Solo ellos vuelven a lanzar", ValorMaximo = highest,
                Detalles = results.Where(item => tiedIds.Contains(item.GetProperty("participant").GetProperty("id").GetString())).Select(DetalleDado).ToList()
            };
        }
        return new ResultadoJuego
        {
            Tipo = TipoResultado.Dados, Titulo = "Los dados decidieron", Subtitulo = "Hoy invita", ValorMaximo = highest,
            Detalles = results.OrderByDescending(item => item.GetProperty("total").GetInt32()).Take(1).Select(DetalleDado).ToList()
        };
    }

    public async Task<OcrResult> LeerFacturaAsync(Stream image, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var imageContent = new StreamContent(image);
        imageContent.Headers.ContentType = new("image/jpeg");
        content.Add(imageContent, "image", fileName);
        using var response = await _http.PostAsync("api/ocr/total", content);
        var json = await LeerAsync(response);
        return new OcrResult
        {
            Total = json.GetProperty("total").GetDecimal(),
            Currency = json.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "USD" : "USD",
            Confidence = json.GetProperty("confidence").GetDouble()
        };
    }

    private async Task<JsonElement> EnviarJuegoAsync(string codigo, string token, string game, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/rooms/{codigo}/games/{game}") { Content = JsonContent.Create(body) };
        request.Headers.Add("x-host-token", token);
        using var response = await _http.SendAsync(request);
        return await LeerAsync(response);
    }

    private static DetallePago DetalleDado(JsonElement item) => new()
    {
        ParticipanteId = item.GetProperty("participant").GetProperty("id").GetString()!,
        Nombre = item.GetProperty("participant").GetProperty("name").GetString()!,
        Dados = item.GetProperty("rolls").EnumerateArray().Select(roll => roll.GetInt32()).ToList(),
        Descripcion = $"Resultado: {item.GetProperty("total").GetInt32()}"
    };

    private static async Task<JsonElement> LeerAsync(HttpResponseMessage response, CancellationToken token = default)
    {
        var text = await response.Content.ReadAsStringAsync(token);
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        if (!response.IsSuccessStatusCode)
        {
            var message = document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : response.ReasonPhrase;
            throw new InvalidOperationException(message ?? "No se pudo conectar con La Vaquita");
        }
        return document.RootElement.Clone();
    }
}
