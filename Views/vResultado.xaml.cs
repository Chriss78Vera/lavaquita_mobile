using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vResultado : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService? _realtime;
    private readonly PartidaLocalStore? _localStore;
    private CancellationTokenSource? _gameAnimationCancellation;
    private Task _gameAnimationTask = Task.CompletedTask;
    private bool _visible;
    private int _revisionJuego;
    private static readonly Color DefaultPageColor = Color.FromArgb("#FBFAF2");

    public vResultado(VaquitaApi api, AppSession session, SalaRealtimeService? realtime = null, PartidaLocalStore? localStore = null)
    {
        InitializeComponent(); _api = api; _session = session; _realtime = realtime; _localStore = localStore;
        ParticipanteActions.IsVisible = session.EsCompartida && !session.EsAnfitrion;
        AnfitrionActions.IsVisible = session.EsCompartida && session.EsAnfitrion;
        NuevaLocalButton.IsVisible = !session.EsCompartida;
        Mostrar();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        if (_session.EsCompartida && _realtime is not null)
        {
            _realtime.JuegoRecibido += Realtime_JuegoRecibido;
            _realtime.JuegoIniciado += Realtime_JuegoIniciado;
            _realtime.SalaFinalizada += Realtime_SalaFinalizada;
        }
        if (!string.IsNullOrWhiteSpace(_session.JuegoEnCurso)) IniciarAnimacionJuego(_session.JuegoEnCurso);
        if (_session.EsCompartida && !_session.EsAnfitrion && _realtime?.UltimoJuego is { } juego)
            Realtime_JuegoRecibido(juego);
    }

    protected override void OnDisappearing()
    {
        _visible = false;
        _revisionJuego++;
        if (_realtime is not null)
        {
            _realtime.JuegoRecibido -= Realtime_JuegoRecibido;
            _realtime.JuegoIniciado -= Realtime_JuegoIniciado;
            _realtime.SalaFinalizada -= Realtime_SalaFinalizada;
        }
        _gameAnimationCancellation?.Cancel();
        base.OnDisappearing();
    }

    private void Realtime_SalaFinalizada(string mensaje) => MainThread.BeginInvokeOnMainThread(async () =>
    {
        if (_session.EsAnfitrion) return;
        await DisplayAlertAsync("Sala finalizada", mensaje, "Aceptar");
        _session.Reiniciar();
        await Navigation.PopToRootAsync();
    });

    private void Realtime_JuegoIniciado(string type) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (_visible) IniciarAnimacionJuego(type);
    });

    private void Realtime_JuegoRecibido(System.Text.Json.JsonElement game) => MainThread.BeginInvokeOnMainThread(async () =>
    {
        if (!_visible) return;
        var eventId = game.TryGetProperty("id", out var idNode) ? idNode.GetString() ?? string.Empty : string.Empty;
        if (!string.IsNullOrWhiteSpace(eventId) && eventId == _session.UltimoEventoId) return;
        var revision = ++_revisionJuego;
        try { await _gameAnimationTask; } catch (OperationCanceledException) { }
        if (!_visible || revision != _revisionJuego) return;
        _session.UltimoEventoId = eventId;
        _session.JuegoEnCurso = string.Empty;
        _session.UltimoResultado = _api.ConvertirEvento(game);
        Mostrar();
    });

    private void IniciarAnimacionJuego(string type)
    {
        _revisionJuego++;
        _gameAnimationCancellation?.Cancel();
        _gameAnimationCancellation = new CancellationTokenSource();
        _session.JuegoEnCurso = type;
        _gameAnimationTask = AnimarJuegoAsync(type, _gameAnimationCancellation.Token);
    }

    private async Task AnimarJuegoAsync(string type, CancellationToken token)
    {
        var isDice = type == "dice";
        HeroImage.Source = "roulette.png";
        HeroImage.IsVisible = !isDice;
        HeroDiceFace.IsVisible = isDice;
        HeroDiceFace.ShowEquals = false;
        ValorMaximoPanel.IsVisible = false;
        DetallesLayout.Children.Clear();
        DesempateButton.IsVisible = ParticipanteActions.IsVisible = AnfitrionActions.IsVisible = NuevaLocalButton.IsVisible = false;
        EyebrowLabel.Text = isDice ? "DADOS EN EL AIRE" : "LA RULETA ESTÁ GIRANDO";
        TituloLabel.Text = isDice ? "Lanzando los dados" : "Buscando a los valientes";
        SubtituloLabel.Text = "Todos en la sala lo están viendo";
        VisualElement animatedHero = isDice ? HeroDiceFace : HeroImage;

        for (var index = 0; index < 12; index++)
        {
            token.ThrowIfCancellationRequested();
            BackgroundColor = isDice ? DefaultPageColor : GameColorPalette.ForIndex(index);
            if (isDice) HeroDiceFace.Value = Random.Shared.Next(1, 7);
            await Task.WhenAll(
                animatedHero.RotateToAsync(animatedHero.Rotation + (isDice ? 95 : 150), 130, Easing.CubicInOut),
                animatedHero.ScaleToAsync(index % 2 == 0 ? 1.1 : 0.94, 130, Easing.CubicInOut),
                animatedHero.TranslateToAsync(isDice ? (index % 2 == 0 ? 14 : -14) : 0, isDice ? -12 : 0, 130, Easing.CubicInOut));
        }

        await Task.WhenAll(
            animatedHero.RotateToAsync(0, 150, Easing.CubicOut),
            animatedHero.ScaleToAsync(1, 150, Easing.CubicOut),
            animatedHero.TranslateToAsync(0, 0, 150, Easing.CubicOut));
        BackgroundColor = DefaultPageColor;
    }

    private void Mostrar()
    {
        var result = _session.UltimoResultado;
        if (result is null) return;
        BackgroundColor = DefaultPageColor;
        ParticipanteActions.IsVisible = _session.EsCompartida && !_session.EsAnfitrion;
        AnfitrionActions.IsVisible = _session.EsCompartida && _session.EsAnfitrion;
        NuevaLocalButton.IsVisible = !_session.EsCompartida;
        TituloLabel.Text = result.Titulo; SubtituloLabel.Text = result.Subtitulo; DetallesLayout.Children.Clear();
        EyebrowLabel.Text = result.Tipo switch
        {
            TipoResultado.Division => "LISTO, FACILITO",
            TipoResultado.Ruleta => "LA SUERTE HABLÓ",
            TipoResultado.Empate => "MUERTE SÚBITA",
            _ => "LOS DADOS DECIDIERON"
        };
        var isDiceResult = result.Tipo is TipoResultado.Dados or TipoResultado.Empate;
        HeroImage.Source = result.Tipo switch
        {
            TipoResultado.Ruleta => "roulette.png",
            _ => "vaquita_mascot.png"
        };
        HeroImage.IsVisible = !isDiceResult;
        HeroDiceFace.IsVisible = isDiceResult;
        HeroDiceFace.ShowEquals = result.Tipo == TipoResultado.Empate;
        HeroDiceFace.Value = result.ValorMaximo ?? 1;
        ValorMaximoPanel.IsVisible = result.ValorMaximo.HasValue;
        ValorMaximoLabel.Text = result.ValorMaximo?.ToString() ?? string.Empty;
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
                BackgroundColor = result.Tipo == TipoResultado.Ruleta
                    ? GameColorPalette.ForParticipant(item.ParticipanteId, _session.Participantes)
                    : result.Tipo is TipoResultado.Dados or TipoResultado.Empate
                        ? Color.FromArgb("#FFD84D")
                        : Color.FromArgb("#B8F340"),
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
            {
                _session.UltimoResultado = await _api.DadosAsync(_session.CodigoSala, _session.HostToken, ids);
                try { await _gameAnimationTask; } catch (OperationCanceledException) { }
            }
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
                    ValorMaximo = high,
                    Detalles = winners.Select(item => new DetallePago { ParticipanteId = item.person.Id, Nombre = item.person.Nombre, Dados = [item.roll], Descripcion = $"Resultado: {item.roll}" }).ToList()
                };
            }
            if (!_session.EsCompartida && _localStore is not null) await _localStore.GuardarAsync(_session);
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
            await Navigation.PushAsync(new vJuegos(_api, _session, _realtime, _localStore));
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
        if (_localStore is not null) await _localStore.LimpiarAsync();
        _session.Reiniciar();
        await Navigation.PopToRootAsync();
    }
}
