using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vSala : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService _realtime;
    private CancellationTokenSource? _polling;
    private Sala? _sala;
    private bool _eventosSuscritos;
    private bool _navegandoResultado;

    public vSala(VaquitaApi api, AppSession session, SalaRealtimeService realtime)
    {
        InitializeComponent(); _api = api; _session = session; _realtime = realtime;
        CodigoLabel.Text = session.CodigoSala;
        CodigoGrandeLabel.Text = session.CodigoSala;
        QrImage.Source = ImageSource.FromUri(new Uri(api.ObtenerUrlQr(session.CodigoSala)));
        QrPanel.IsVisible = CuentaPanel.IsVisible = CerrarSalaButton.IsVisible = ContinuarButton.IsVisible = session.EsAnfitrion;
        ParticipantePanel.IsVisible = !session.EsAnfitrion;
        EstadoLabel.Text = "CONECTANDO";
        TituloSalaLabel.Text = session.EsAnfitrion ? "Tu sala está lista" : "Sala del anfitrión";
        ContinuarButton.Text = "Elegir cómo pagamos";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SuscribirEventos();
        IniciarActualizacion();
        await ActivarTiempoRealAsync();
    }

    protected override void OnDisappearing()
    {
        _polling?.Cancel();
        DesuscribirEventos();
        base.OnDisappearing();
    }

    private void SuscribirEventos()
    {
        if (_eventosSuscritos) return;
        _realtime.SalaActualizada += Realtime_SalaActualizada;
        _realtime.JuegoRecibido += Realtime_JuegoRecibido;
        _realtime.SalaFinalizada += Realtime_SalaFinalizada;
        _realtime.ConexionCambiada += Realtime_ConexionCambiada;
        _eventosSuscritos = true;
    }

    private void DesuscribirEventos()
    {
        if (!_eventosSuscritos) return;
        _realtime.SalaActualizada -= Realtime_SalaActualizada;
        _realtime.JuegoRecibido -= Realtime_JuegoRecibido;
        _realtime.SalaFinalizada -= Realtime_SalaFinalizada;
        _realtime.ConexionCambiada -= Realtime_ConexionCambiada;
        _eventosSuscritos = false;
    }

    private async Task ActivarTiempoRealAsync()
    {
        try
        {
            await ActualizarSalaAsync();
            await _realtime.ConectarAsync(_session.CodigoSala);
        }
        catch (Exception error)
        {
            Realtime_ConexionCambiada(false);
            await DisplayAlertAsync("Conexión limitada", $"Se seguirá intentando sincronizar. {error.Message}", "Aceptar");
        }
    }

    private void IniciarActualizacion()
    {
        _polling?.Cancel(); _polling = new CancellationTokenSource(); var token = _polling.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try { await ActualizarSalaAsync(token); }
                catch (OperationCanceledException) { break; }
                catch { }
                try { await Task.Delay(15000, token); } catch { break; }
            }
        }, token);
    }

    private async Task ActualizarSalaAsync(CancellationToken token = default)
    {
        var sala = await _api.ObtenerSalaAsync(_session.CodigoSala, token);
        await MainThread.InvokeOnMainThreadAsync(() => MostrarSala(sala));
    }

    private void Realtime_SalaActualizada() => _ = Task.Run(async () =>
    {
        try { await ActualizarSalaAsync(); } catch { }
    });

    private void Realtime_JuegoRecibido(System.Text.Json.JsonElement game) =>
        MainThread.BeginInvokeOnMainThread(async () => await MostrarResultadoAsync(game));

    private void Realtime_ConexionCambiada(bool conectado) => MainThread.BeginInvokeOnMainThread(() =>
    {
        EstadoLabel.Text = conectado
            ? (_session.EsAnfitrion ? "ANFITRIÓN EN VIVO" : "CONECTADO EN VIVO")
            : "RECONECTANDO";
        SyncTitleLabel.Text = conectado ? "Todo sincronizado" : "Buscando conexión";
        SyncDetailLabel.Text = conectado
            ? "Los cambios aparecerán para todos al instante"
            : "Conservamos el último estado mientras vuelves";
    });

    private void Realtime_SalaFinalizada(string mensaje) => MainThread.BeginInvokeOnMainThread(async () =>
    {
        await DisplayAlertAsync("Sala finalizada", mensaje, "Aceptar");
        _session.Reiniciar();
        await Navigation.PopToRootAsync();
    });

    private void MostrarSala(Sala sala)
    {
        _sala = sala; _session.Participantes = sala.Participantes; _session.Cuenta = sala.Cuenta;
        ConteoLabel.Text = $"{sala.Participantes.Count} participantes conectados";
        ParticipantesLayout.Children.Clear();
        string[] colors = ["#B8F340", "#24D6C8", "#FFD84D", "#6D8DFF", "#FF8A5B"];
        int index = 0;
        foreach (var persona in sala.Participantes)
        {
            ParticipantesLayout.Children.Add(new Border
            {
                WidthRequest = 112, HeightRequest = 116, BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#DCE5DB"), Padding = 10,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                Content = new VerticalStackLayout
                {
                    Spacing = 4, VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Border
                        {
                            WidthRequest = 47, HeightRequest = 47, Padding = 0,
                            BackgroundColor = Color.FromArgb(colors[index++ % colors.Length]), Stroke = Color.FromArgb("#163832"), StrokeThickness = 2,
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
                            Content = new Label { Text = persona.Nombre[..1].ToUpperInvariant(), FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#163832"), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
                        },
                        new Label { Text = persona.Nombre, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#163832"), HorizontalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.TailTruncation },
                        new Label { Text = persona.EsAnfitrion ? "Anfitrión" : "Listo", FontSize = 9, TextColor = Color.FromArgb("#6B7C78"), HorizontalTextAlignment = TextAlignment.Center }
                    }
                }
            });
        }
        if (sala.Cuenta.Total > 0)
        {
            TotalConfirmadoPanel.IsVisible = true; TotalConfirmadoLabel.Text = MoneyFormat.Format(sala.Cuenta.Total, sala.Cuenta.Moneda);
            ContinuarButton.IsEnabled = true;
            if (!_session.EsAnfitrion)
                ParticipanteEsperaLabel.Text = "Cuenta confirmada. El resultado aparecerá aquí apenas el anfitrión juegue.";
        }
        else ContinuarButton.IsEnabled = false;

        if (!_session.EsAnfitrion && sala.UltimoEvento.HasValue)
            _ = MostrarResultadoAsync(sala.UltimoEvento.Value);
    }

    private async Task MostrarResultadoAsync(System.Text.Json.JsonElement game)
    {
        if (_session.EsAnfitrion || _navegandoResultado) return;
        var eventId = game.TryGetProperty("id", out var idNode) ? idNode.GetString() ?? string.Empty : string.Empty;
        if (!string.IsNullOrWhiteSpace(eventId) && eventId == _session.UltimoEventoId) return;

        _session.UltimoEventoId = eventId;
        _session.UltimoResultado = _api.ConvertirEvento(game);
        _navegandoResultado = true;
        try { await Navigation.PushAsync(new vResultado(_api, _session, _realtime)); }
        finally { _navegandoResultado = false; }
    }

    private async void Camara_Clicked(object? sender, EventArgs e) => await SeleccionarFactura(true);
    private async void Galeria_Clicked(object? sender, EventArgs e) => await SeleccionarFactura(false);

    private async void CopiarCodigo_Clicked(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_session.CodigoSala);
        await DisplayAlertAsync("Código copiado", "Comparte este código con tu grupo.", "Aceptar");
    }

    private async Task SeleccionarFactura(bool camara)
    {
        try
        {
            FileResult? foto;
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                foto = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = camara ? "Selecciona una foto de la factura" : "Selecciona la factura"
                });
            }
            else
            {
                foto = camara
                    ? await MediaPicker.Default.CapturePhotoAsync()
                    : (await MediaPicker.Default.PickPhotosAsync()).FirstOrDefault();
            }
            if (foto is null) return;
            Cargando.IsVisible = Cargando.IsRunning = true;
            await using var stream = await foto.OpenReadAsync();
            var ocr = await _api.LeerFacturaAsync(stream, foto.FileName);
            var cuentaDetectada = new Cuenta { Total = ocr.Total, Moneda = ocr.Currency, FueDetectadaPorOcr = true, ConfianzaOcr = ocr.Confidence };
            await Navigation.PushAsync(new vRevisarCuenta(_api, _session, cuentaDetectada));
        }
        catch (PermissionException) { await DisplayAlertAsync("Permiso rechazado", "Puedes usar la galería o escribir el total.", "Aceptar"); }
        catch (Exception error) { await DisplayAlertAsync("No se pudo leer", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void ConfirmarTotal_Clicked(object? sender, EventArgs e)
    {
        if (!MoneyFormat.TryParse(TotalEntry.Text, out var total) || total <= 0)
        { await DisplayAlertAsync("Monto inválido", "Ingresa un total mayor que cero.", "Aceptar"); return; }
        _session.Cuenta = new Cuenta { Total = total, Moneda = "USD" };
        await GuardarCuenta();
    }

    private async Task GuardarCuenta()
    {
        try
        {
            Cargando.IsVisible = Cargando.IsRunning = true;
            await _api.ConfirmarCuentaAsync(_session.CodigoSala, _session.HostToken, _session.Cuenta);
            TotalConfirmadoPanel.IsVisible = true; TotalConfirmadoLabel.Text = MoneyFormat.Format(_session.Cuenta.Total, _session.Cuenta.Moneda);
            ContinuarButton.IsEnabled = true;
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo guardar", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void Continuar_Clicked(object? sender, EventArgs e)
    {
        if (_sala is null || _sala.Cuenta.Total <= 0) { await DisplayAlertAsync("Aún no hay total", "El anfitrión debe confirmar la cuenta.", "Aceptar"); return; }
        _session.Cuenta = _sala.Cuenta; _session.Participantes = _sala.Participantes;
        await Navigation.PushAsync(new vJuegos(_api, _session, _realtime));
    }

    private async void CerrarSala_Clicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Cerrar sala", "El código dejará de funcionar y nadie más podrá ingresar.", "Cerrar", "Cancelar")) return;
        try
        {
            await _api.CerrarSalaAsync(_session.CodigoSala, _session.HostToken);
            _session.Reiniciar();
            await Navigation.PopToRootAsync();
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo cerrar", error.Message, "Aceptar"); }
    }
}
