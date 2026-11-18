using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vJuegos : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;

    public vJuegos(VaquitaApi api, AppSession session)
    {
        InitializeComponent(); _api = api; _session = session;
        ResumenLabel.Text = $"Cuenta de {session.Cuenta.Total:N2} {session.Cuenta.Moneda} · {session.Participantes.Count} personas";
        DivisionHintLabel.Text = $"Aprox. {session.Cuenta.Total / Math.Max(1, session.Participantes.Count):N2} por persona";
        if (session.EsCompartida && !session.EsAnfitrion)
        {
            DividirCard.IsVisible = RuletaCard.IsVisible = DadosCard.IsVisible = false; EsperaPanel.IsVisible = true;
        }
    }

    private async void Dividir_Clicked(object? sender, EventArgs e)
    {
        await Ejecutar(async () =>
        {
            _session.UltimoResultado = _session.EsCompartida
                ? await _api.DividirAsync(_session.CodigoSala, _session.HostToken)
                : DividirLocal();
        });
    }

    private ResultadoJuego DividirLocal()
    {
        var cents = (int)Math.Round(_session.Cuenta.Total * 100, MidpointRounding.AwayFromZero);
        var each = cents / _session.Participantes.Count; var remainder = cents % _session.Participantes.Count;
        return new ResultadoJuego
        {
            Tipo = TipoResultado.Division, Titulo = "A cada uno le toca", Subtitulo = "Los centavos coinciden con el total",
            Detalles = _session.Participantes.Select((person, index) => new DetallePago
            { ParticipanteId = person.Id, Nombre = person.Nombre, Monto = (each + (index < remainder ? 1 : 0)) / 100m }).ToList()
        };
    }

    private async void Ruleta_Clicked(object? sender, EventArgs e)
    {
        var max = Math.Max(1, _session.Participantes.Count - 1);
        var answer = await DisplayPromptAsync("Ruleta", $"¿Cuántas personas pagan? 1 a {max}", "Girar", "Cancelar", keyboard: Keyboard.Numeric, initialValue: "1");
        if (!int.TryParse(answer, out var count) || count < 1 || count > max) return;
        await Ejecutar(async () =>
        {
            if (_session.EsCompartida) _session.UltimoResultado = await _api.RuletaAsync(_session.CodigoSala, _session.HostToken, count);
            else
            {
                var payers = _session.Participantes.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
                _session.UltimoResultado = new ResultadoJuego
                {
                    Tipo = TipoResultado.Ruleta, Titulo = "La suerte habló", Subtitulo = "Ellos pagan esta vez",
                    Detalles = payers.Select(person => new DetallePago { ParticipanteId = person.Id, Nombre = person.Nombre, Monto = _session.Cuenta.Total / count }).ToList()
                };
            }
        });
    }

    private async void Dados_Clicked(object? sender, EventArgs e)
    {
        await Ejecutar(async () =>
        {
            if (_session.EsCompartida) _session.UltimoResultado = await _api.DadosAsync(_session.CodigoSala, _session.HostToken);
            else
            {
                var rolls = _session.Participantes.Select(person => (person, roll: Random.Shared.Next(1, 7))).ToList();
                var high = rolls.Max(item => item.roll); var winners = rolls.Where(item => item.roll == high).ToList();
                _session.UltimoResultado = new ResultadoJuego
                {
                    Tipo = winners.Count > 1 ? TipoResultado.Empate : TipoResultado.Dados,
                    Titulo = winners.Count > 1 ? "¡Tenemos empate!" : "Los dados decidieron",
                    Subtitulo = winners.Count > 1 ? "Vuelvan a lanzar" : "Hoy invita",
                    Detalles = winners.Select(item => new DetallePago { ParticipanteId = item.person.Id, Nombre = item.person.Nombre, Dados = [item.roll], Descripcion = $"Resultado: {item.roll}" }).ToList()
                };
            }
        });
    }

    private async Task Ejecutar(Func<Task> action)
    {
        Cargando.IsVisible = Cargando.IsRunning = true;
        try { await action(); await Navigation.PushAsync(new vResultado(_api, _session)); }
        catch (Exception error) { await DisplayAlertAsync("No se pudo jugar", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }
}
