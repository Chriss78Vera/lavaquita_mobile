using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vDados : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;
    private readonly PartidaLocalStore? _localStore;

    public vDados(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null, PartidaLocalStore? localStore = null)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        _localStore = localStore;
        ParticipantesLabel.Text = $"{session.Participantes.Count} participantes lanzarán los dados.";
        if (session.Participantes.Count > 6) ReglaLabel.Text = "Usaremos dos dados por persona.";
    }

    private async void Lanzar_Clicked(object? sender, EventArgs e)
    {
        LanzarButton.IsEnabled = false;
        Cargando.IsVisible = Cargando.IsRunning = true;
        LanzandoLabel.IsVisible = true;
        using var animationCancellation = new CancellationTokenSource();
        var animationTask = AnimarDadosAsync(animationCancellation.Token);
        try
        {
            var minimumAnimation = Task.Delay(1600);
            var resultTask = _session.EsCompartida
                ? _api.DadosAsync(_session.CodigoSala, _session.HostToken)
                : Task.FromResult(CrearResultadoLocal(_session.Participantes));
            _session.UltimoResultado = await resultTask;
            await minimumAnimation;
            if (!_session.EsCompartida && _localStore is not null) await _localStore.GuardarAsync(_session);
            await Navigation.PushAsync(new vResultado(_api, _session, _realtime, _localStore));
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("No se pudo lanzar", error.Message, "Aceptar");
        }
        finally
        {
            animationCancellation.Cancel();
            try { await animationTask; } catch (OperationCanceledException) { }
            await RestablecerDadoAsync();
            LanzarButton.IsEnabled = true;
            Cargando.IsVisible = Cargando.IsRunning = false;
            LanzandoLabel.IsVisible = false;
            DiceFaceView.Value = 1;
        }
    }

    private async Task AnimarDadosAsync(CancellationToken token)
    {
        var direction = 1;
        while (!token.IsCancellationRequested)
        {
            DiceFaceView.Value = Random.Shared.Next(1, 7);
            await Task.WhenAll(
                DiceFaceView.RotateToAsync(DiceFaceView.Rotation + (110 * direction), 120, Easing.CubicInOut),
                DiceFaceView.TranslateToAsync(18 * direction, -22, 120, Easing.CubicOut),
                DiceFaceView.ScaleToAsync(1.12, 120, Easing.CubicOut));
            token.ThrowIfCancellationRequested();

            DiceFaceView.Value = Random.Shared.Next(1, 7);
            await Task.WhenAll(
                DiceFaceView.RotateToAsync(DiceFaceView.Rotation + (140 * direction), 120, Easing.CubicInOut),
                DiceFaceView.TranslateToAsync(-14 * direction, 8, 120, Easing.BounceOut),
                DiceFaceView.ScaleToAsync(0.94, 120, Easing.CubicIn));
            direction *= -1;
        }
    }

    private Task RestablecerDadoAsync() => Task.WhenAll(
        DiceFaceView.RotateToAsync(0, 160, Easing.CubicOut),
        DiceFaceView.TranslateToAsync(0, 0, 160, Easing.CubicOut),
        DiceFaceView.ScaleToAsync(1, 160, Easing.CubicOut));

    private static ResultadoJuego CrearResultadoLocal(IReadOnlyList<Participante> participantes)
    {
        var rolls = participantes.Select(person => (person, roll: Random.Shared.Next(1, 7))).ToList();
        var high = rolls.Max(item => item.roll);
        var winners = rolls.Where(item => item.roll == high).ToList();
        return new ResultadoJuego
        {
            Tipo = winners.Count > 1 ? TipoResultado.Empate : TipoResultado.Dados,
            Titulo = winners.Count > 1 ? "¡Tenemos empate!" : "Los dados decidieron",
            Subtitulo = winners.Count > 1 ? "Solo ellos vuelven a lanzar" : "Hoy invita",
            ValorMaximo = high,
            Detalles = winners.Select(item => new DetallePago
            {
                ParticipanteId = item.person.Id,
                Nombre = item.person.Nombre,
                Dados = [item.roll],
                Descripcion = $"Resultado: {item.roll}"
            }).ToList()
        };
    }
}
