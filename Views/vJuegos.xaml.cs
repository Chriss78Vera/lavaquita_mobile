using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vJuegos : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;
    private readonly PartidaLocalStore? _localStore;

    public vJuegos(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null, PartidaLocalStore? localStore = null)
    {
        InitializeComponent(); _api = api; _session = session; _realtime = realtime; _localStore = localStore;
        ResumenLabel.Text = $"Cuenta de {MoneyFormat.Format(session.Cuenta.Total, session.Cuenta.Moneda)} · {session.Participantes.Count} personas";
        DivisionHintLabel.Text = $"Aprox. {MoneyFormat.Format(session.Cuenta.Total / Math.Max(1, session.Participantes.Count), session.Cuenta.Moneda)} por persona";
        if (session.EsCompartida && !session.EsAnfitrion)
        {
            DividirCard.IsVisible = RuletaCard.IsVisible = DadosCard.IsVisible = false; EsperaPanel.IsVisible = true;
        }
    }

    private async void Dividir_Clicked(object? sender, EventArgs e)
    {
        if (_session.Participantes.Count < 2)
        {
            await DisplayAlertAsync("Faltan participantes", "Para dividir la cuenta necesitas al menos 2 personas.", "Aceptar");
            return;
        }
        await Ejecutar(async () =>
        {
            _session.UltimoResultado = _session.EsCompartida
                ? await _api.DividirAsync(_session.CodigoSala, _session.HostToken)
                : DividirLocal();
        });
    }

    private void DividirCard_Tapped(object? sender, TappedEventArgs e) => Dividir_Clicked(sender, e);

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
        await Navigation.PushAsync(new vRuleta(_api, _session, _realtime, _localStore));
    }

    private void RuletaCard_Tapped(object? sender, TappedEventArgs e) => Ruleta_Clicked(sender, e);

    private async void Dados_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new vDados(_api, _session, _realtime, _localStore));
    }

    private void DadosCard_Tapped(object? sender, TappedEventArgs e) => Dados_Clicked(sender, e);

    private async Task Ejecutar(Func<Task> action)
    {
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            await action();
            if (!_session.EsCompartida && _localStore is not null) await _localStore.GuardarAsync(_session);
            await Navigation.PushAsync(new vResultado(_api, _session, _realtime, _localStore));
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo jugar", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }
}
