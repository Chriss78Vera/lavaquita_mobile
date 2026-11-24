using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vDados : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;

    public vDados(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        ParticipantesLabel.Text = $"{session.Participantes.Count} participantes lanzarán los dados.";
        if (session.Participantes.Count > 6) ReglaLabel.Text = "Usaremos dos dados por persona.";
    }

    private async void Lanzar_Clicked(object? sender, EventArgs e)
    {
        LanzarButton.IsEnabled = false;
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            _session.UltimoResultado = _session.EsCompartida
                ? await _api.DadosAsync(_session.CodigoSala, _session.HostToken)
                : CrearResultadoLocal(_session.Participantes);
            await Navigation.PushAsync(new vResultado(_api, _session, _realtime));
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("No se pudo lanzar", error.Message, "Aceptar");
        }
        finally
        {
            LanzarButton.IsEnabled = true;
            Cargando.IsVisible = Cargando.IsRunning = false;
        }
    }

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
