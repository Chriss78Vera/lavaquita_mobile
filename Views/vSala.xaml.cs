using System.Globalization;
using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vSala : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private CancellationTokenSource? _polling;
    private Sala? _sala;

    public vSala(VaquitaApi api, AppSession session)
    {
        InitializeComponent(); _api = api; _session = session;
        CodigoLabel.Text = session.CodigoSala;
        CodigoGrandeLabel.Text = session.CodigoSala;
        QrImage.Source = ImageSource.FromUri(new Uri(api.ObtenerUrlQr(session.CodigoSala)));
        CuentaPanel.IsVisible = session.EsAnfitrion;
        CerrarSalaButton.IsVisible = session.EsAnfitrion;
        EstadoLabel.Text = session.EsAnfitrion ? "ANFITRIÓN EN VIVO" : "CONECTADO EN VIVO";
        ContinuarButton.Text = session.EsAnfitrion ? "Elegir cómo pagamos" : "Ver juego o resultado";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing(); IniciarActualizacion();
    }

    protected override void OnDisappearing()
    {
        _polling?.Cancel(); base.OnDisappearing();
    }

    private void IniciarActualizacion()
    {
        _polling?.Cancel(); _polling = new CancellationTokenSource(); var token = _polling.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var sala = await _api.ObtenerSalaAsync(_session.CodigoSala, token);
                    await MainThread.InvokeOnMainThreadAsync(() => MostrarSala(sala));
                }
                catch (OperationCanceledException) { break; }
                catch { }
                try { await Task.Delay(5000, token); } catch { break; }
            }
        }, token);
    }

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
            TotalConfirmadoPanel.IsVisible = true; TotalConfirmadoLabel.Text = $"{sala.Cuenta.Total:N2} {sala.Cuenta.Moneda}";
            ContinuarButton.IsEnabled = true;
        }
        else ContinuarButton.IsEnabled = false;
    }

    private async void Camara_Clicked(object? sender, EventArgs e) => await SeleccionarFactura(true);
    private async void Galeria_Clicked(object? sender, EventArgs e) => await SeleccionarFactura(false);

    private async Task SeleccionarFactura(bool camara)
    {
        try
        {
            FileResult? foto = camara ? await MediaPicker.Default.CapturePhotoAsync() : (await MediaPicker.Default.PickPhotosAsync()).FirstOrDefault();
            if (foto is null) return;
            Cargando.IsVisible = Cargando.IsRunning = true;
            await using var stream = await foto.OpenReadAsync();
            var ocr = await _api.LeerFacturaAsync(stream, foto.FileName);
            if (await DisplayAlertAsync("Total detectado", $"{ocr.Total:N2} {ocr.Currency}\nConfianza: {ocr.Confidence:P0}", "Usar", "Cancelar"))
            {
                _session.Cuenta = new Cuenta { Total = ocr.Total, Moneda = ocr.Currency, FueDetectadaPorOcr = true, ConfianzaOcr = ocr.Confidence };
                await GuardarCuenta();
            }
        }
        catch (PermissionException) { await DisplayAlertAsync("Permiso rechazado", "Puedes usar la galería o escribir el total.", "Aceptar"); }
        catch (Exception error) { await DisplayAlertAsync("No se pudo leer", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void ConfirmarTotal_Clicked(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(TotalEntry.Text?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var total) || total <= 0)
        { await DisplayAlertAsync("Monto inválido", "Ingresa un total mayor que cero.", "Aceptar"); return; }
        _session.Cuenta = new Cuenta { Total = total, Moneda = "COP" };
        await GuardarCuenta();
    }

    private async Task GuardarCuenta()
    {
        try
        {
            Cargando.IsVisible = Cargando.IsRunning = true;
            await _api.ConfirmarCuentaAsync(_session.CodigoSala, _session.HostToken, _session.Cuenta);
            TotalConfirmadoPanel.IsVisible = true; TotalConfirmadoLabel.Text = $"{_session.Cuenta.Total:N2} {_session.Cuenta.Moneda}";
            ContinuarButton.IsEnabled = true;
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo guardar", error.Message, "Aceptar"); }
        finally { Cargando.IsVisible = Cargando.IsRunning = false; }
    }

    private async void Continuar_Clicked(object? sender, EventArgs e)
    {
        if (_sala is null || _sala.Cuenta.Total <= 0) { await DisplayAlertAsync("Aún no hay total", "El anfitrión debe confirmar la cuenta.", "Aceptar"); return; }
        _session.Cuenta = _sala.Cuenta; _session.Participantes = _sala.Participantes;
        if (!_session.EsAnfitrion && _sala.UltimoEvento.HasValue)
        {
            _session.UltimoResultado = _api.ConvertirEvento(_sala.UltimoEvento.Value);
            await Navigation.PushAsync(new vResultado(_api, _session)); return;
        }
        await Navigation.PushAsync(new vJuegos(_api, _session));
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
