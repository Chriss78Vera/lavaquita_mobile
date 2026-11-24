using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vRuleta : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;
    private readonly int _maximo;
    private int _cantidad = 1;

    public vRuleta(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        _maximo = Math.Max(1, session.Participantes.Count - 1);
        MaximoLabel.Text = $"de {_maximo}";
        ActualizarCantidad();
    }

    private void Restar_Clicked(object? sender, EventArgs e)
    {
        _cantidad = Math.Max(1, _cantidad - 1);
        ActualizarCantidad();
    }

    private void Sumar_Clicked(object? sender, EventArgs e)
    {
        _cantidad = Math.Min(_maximo, _cantidad + 1);
        ActualizarCantidad();
    }

    private void ActualizarCantidad()
    {
        CantidadLabel.Text = _cantidad.ToString();
        AyudaLabel.Text = _cantidad == 1
            ? "Elegiremos a una persona valiente."
            : $"Elegiremos a {_cantidad} personas valientes.";
    }

    private async void Girar_Clicked(object? sender, EventArgs e)
    {
        GirarButton.IsEnabled = false;
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            _session.UltimoResultado = _session.EsCompartida
                ? await _api.RuletaAsync(_session.CodigoSala, _session.HostToken, _cantidad)
                : CrearResultadoLocal();
            await Navigation.PushAsync(new vResultado(_api, _session, _realtime));
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("No se pudo girar", error.Message, "Aceptar");
        }
        finally
        {
            GirarButton.IsEnabled = true;
            Cargando.IsVisible = Cargando.IsRunning = false;
        }
    }

    private ResultadoJuego CrearResultadoLocal()
    {
        var payers = _session.Participantes.OrderBy(_ => Random.Shared.Next()).Take(_cantidad).ToList();
        return new ResultadoJuego
        {
            Tipo = TipoResultado.Ruleta,
            Titulo = "La suerte habló",
            Subtitulo = "Ellos pagan esta vez",
            Detalles = payers.Select(person => new DetallePago
            {
                ParticipanteId = person.Id,
                Nombre = person.Nombre,
                Monto = _session.Cuenta.Total / _cantidad
            }).ToList()
        };
    }
}
