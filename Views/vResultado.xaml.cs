using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vResultado : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;

    public vResultado(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null)
    {
        InitializeComponent(); _api = api; _session = session; _realtime = realtime;
        ParticipanteActions.IsVisible = session.EsCompartida && !session.EsAnfitrion;
        AnfitrionActions.IsVisible = session.EsCompartida && session.EsAnfitrion;
        NuevaLocalButton.IsVisible = !session.EsCompartida;
        Mostrar();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_session.EsCompartida && _realtime is not null)
        {
            _realtime.JuegoRecibido += Realtime_JuegoRecibido;
            _realtime.SalaFinalizada += Realtime_SalaFinalizada;
        }
    }

    protected override void OnDisappearing()
    {
        if (_realtime is not null)
        {
            _realtime.JuegoRecibido -= Realtime_JuegoRecibido;
            _realtime.SalaFinalizada -= Realtime_SalaFinalizada;
        }
        base.OnDisappearing();
    }

    private void Realtime_SalaFinalizada(string mensaje) => MainThread.BeginInvokeOnMainThread(async () =>
    {
        if (_session.EsAnfitrion) return;
        await DisplayAlertAsync("Sala finalizada", mensaje, "Aceptar");
        _session.Reiniciar();
        await Navigation.PopToRootAsync();
    });

    private void Realtime_JuegoRecibido(System.Text.Json.JsonElement game) => MainThread.BeginInvokeOnMainThread(() =>
    {
        var eventId = game.TryGetProperty("id", out var idNode) ? idNode.GetString() ?? string.Empty : string.Empty;
        if (!string.IsNullOrWhiteSpace(eventId) && eventId == _session.UltimoEventoId) return;
        _session.UltimoEventoId = eventId;
        _session.UltimoResultado = _api.ConvertirEvento(game);
        Mostrar();
    });

    private void Mostrar()
    {
        var result = _session.UltimoResultado;
        if (result is null) return;
        TituloLabel.Text = result.Titulo; SubtituloLabel.Text = result.Subtitulo; DetallesLayout.Children.Clear();
        EyebrowLabel.Text = result.Tipo switch
        {
            TipoResultado.Division => "LISTO, FACILITO",
            TipoResultado.Ruleta => "LA SUERTE HABLÓ",
            TipoResultado.Empate => "MUERTE SÚBITA",
            _ => "LOS DADOS DECIDIERON"
        };
        HeroImage.Source = result.Tipo switch
        {
            TipoResultado.Ruleta => "roulette.png",
            TipoResultado.Dados or TipoResultado.Empate => "dice_big.png",
            _ => "vaquita_mascot.png"
        };
        foreach (var item in result.Detalles)
        {
            var row = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(55), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 10
            };
            row.Add(new Border
            {
                WidthRequest = 48, HeightRequest = 48, Padding = 0,
                BackgroundColor = result.Tipo == TipoResultado.Dados ? Color.FromArgb("#FFD84D") : Color.FromArgb("#B8F340"),
                Stroke = Color.FromArgb("#163832"), StrokeThickness = 2,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
                Content = new Label { Text = item.Nombre[..1].ToUpperInvariant(), FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#163832"), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
            }, 0);
            row.Add(new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = item.Nombre, TextColor = Color.FromArgb("#163832"), FontSize = 17, FontAttributes = FontAttributes.Bold },
                    new Label { Text = item.Dados.Count > 0 ? $"Dados: {string.Join(" + ", item.Dados)}" : "Cuenta asignada", TextColor = Color.FromArgb("#6B7C78"), FontSize = 10 }
                }
            }, 1);
            row.Add(new Label { Text = item.MontoTexto, TextColor = Color.FromArgb("#287C70"), FontSize = 16, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center }, 2);
            DetallesLayout.Children.Add(new Border
            {
                BackgroundColor = Colors.White, Stroke = Color.FromArgb("#163832"), StrokeThickness = 2, Padding = 11,
                Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#163832")), Offset = new Point(3, 4), Radius = 0, Opacity = 1 },
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 }, Content = row
            });
        }
        DesempateButton.IsVisible = result.Tipo == TipoResultado.Empate && (!_session.EsCompartida || _session.EsAnfitrion);
    }

    private async void Desempate_Clicked(object? sender, EventArgs e)
    {
        var ids = _session.UltimoResultado?.Detalles.Select(item => item.ParticipanteId).ToArray() ?? [];
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            if (_session.EsCompartida)
                _session.UltimoResultado = await _api.DadosAsync(_session.CodigoSala, _session.HostToken, ids);
            else
            {
                var participants = _session.Participantes.Where(item => ids.Contains(item.Id)).ToList();
                var rolls = participants.Select(person => (person, roll: Random.Shared.Next(1, 7))).ToList();
                var high = rolls.Max(item => item.roll); var winners = rolls.Where(item => item.roll == high).ToList();
                _session.UltimoResultado = new ResultadoJuego
                {
                    Tipo = winners.Count > 1 ? TipoResultado.Empate : TipoResultado.Dados,
                    Titulo = winners.Count > 1 ? "¡Tenemos empate!" : "Los dados decidieron",
                    Subtitulo = winners.Count > 1 ? "Vuelvan a lanzar" : "Hoy invita",
                    Detalles = winners.Select(item => new DetallePago { ParticipanteId = item.person.Id, Nombre = item.person.Nombre, Dados = [item.roll], Descripcion = $"Resultado: {item.roll}" }).ToList()
                };
            }
            Mostrar();
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo desempatar", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void OtroJuego_Clicked(object? sender, EventArgs e)
    {
        try
        {
            var sala = await _api.ObtenerSalaAsync(_session.CodigoSala);
            _session.Participantes = sala.Participantes;
            _session.Cuenta = sala.Cuenta;
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("No se pudo actualizar", error.Message, "Aceptar");
            return;
        }
        _session.UltimoResultado = null;
        _session.UltimoEventoId = string.Empty;
        var stack = Navigation.NavigationStack.ToList();
        var juegosIndex = stack.FindLastIndex(page => page is vJuegos);
        if (juegosIndex < 0)
        {
            await Navigation.PushAsync(new vJuegos(_api, _session, _realtime));
            return;
        }

        foreach (var page in stack.Skip(juegosIndex + 1).SkipLast(1).ToList())
            Navigation.RemovePage(page);
        await Navigation.PopAsync();
    }

    private async void FinalizarSala_Clicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Finalizar sala", "Todos los participantes saldrán de la sala.", "Finalizar", "Cancelar")) return;
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            await _api.CerrarSalaAsync(_session.CodigoSala, _session.HostToken);
            await (_realtime?.DesconectarAsync() ?? Task.CompletedTask);
            _session.Reiniciar();
            await Navigation.PopToRootAsync();
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo finalizar", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void SalirSala_Clicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Salir de la sala", "Dejarás de recibir los próximos resultados.", "Salir", "Cancelar")) return;
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            await _api.SalirSalaAsync(_session.CodigoSala, _session.ParticipantToken);
            await (_realtime?.DesconectarAsync() ?? Task.CompletedTask);
            _session.Reiniciar();
            await Navigation.PopToRootAsync();
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo salir", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void NuevaLocal_Clicked(object? sender, EventArgs e)
    {
        _session.Reiniciar();
        await Navigation.PopToRootAsync();
    }
}
