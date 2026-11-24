using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vRuleta : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;
    private readonly PartidaLocalStore? _localStore;
    private readonly int _maximo;
    private int _cantidad = 1;

    public vRuleta(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null, PartidaLocalStore? localStore = null)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        _localStore = localStore;
        _maximo = Math.Max(1, session.Participantes.Count - 1);
        MaximoLabel.Text = $"de {_maximo}";
        CrearColoresParticipantes();
        ActualizarCantidad();
    }

    private void CrearColoresParticipantes()
    {
        ParticipantesColoresLayout.Children.Clear();
        for (var index = 0; index < _session.Participantes.Count; index++)
        {
            var participante = _session.Participantes[index];
            ParticipantesColoresLayout.Children.Add(new Border
            {
                Margin = new Thickness(4), Padding = new Thickness(10, 6),
                BackgroundColor = GameColorPalette.ForIndex(index), Stroke = Color.FromArgb("#163832"), StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 99 },
                Content = new Label { Text = participante.Nombre, FontSize = 10, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#163832") }
            });
        }
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
        NombreGirandoLabel.IsVisible = true;
        using var animationCancellation = new CancellationTokenSource();
        var animationTask = AnimarRuletaAsync(animationCancellation.Token);
        try
        {
            var resultTask = _session.EsCompartida
                ? _api.RuletaAsync(_session.CodigoSala, _session.HostToken, _cantidad)
                : Task.FromResult(CrearResultadoLocal());
            _session.UltimoResultado = await resultTask;
            await animationTask;
            if (!_session.EsCompartida && _localStore is not null) await _localStore.GuardarAsync(_session);
            await Navigation.PushAsync(new vResultado(_api, _session, _realtime, _localStore));
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("No se pudo girar", error.Message, "Aceptar");
        }
        finally
        {
            animationCancellation.Cancel();
            try { await animationTask; } catch (OperationCanceledException) { }
            BackgroundColor = Color.FromArgb("#FBFAF2");
            RuletaImage.Rotation = 0;
            NombreGirandoLabel.IsVisible = false;
            GirarButton.IsEnabled = true;
            Cargando.IsVisible = Cargando.IsRunning = false;
        }
    }

    private async Task AnimarRuletaAsync(CancellationToken token)
    {
        var totalSteps = Math.Max(15, _session.Participantes.Count * 3);
        for (var step = 0; step < totalSteps; step++)
        {
            token.ThrowIfCancellationRequested();
            var participantIndex = step % _session.Participantes.Count;
            BackgroundColor = GameColorPalette.ForIndex(participantIndex);
            NombreGirandoLabel.Text = _session.Participantes[participantIndex].Nombre;
            await RuletaImage.RotateToAsync(RuletaImage.Rotation + 150, (uint)(120 + (step * 4)), Easing.CubicInOut);
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
