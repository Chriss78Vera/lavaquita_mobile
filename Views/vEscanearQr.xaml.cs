using LaVaquita.Utils;
using ZXing.Net.Maui;

namespace LaVaquita.Views;

public partial class vEscanearQr : ContentPage
{
    private readonly Action<string> _codigoDetectado;
    private int _procesando;

    public vEscanearQr(Action<string> codigoDetectado)
    {
        InitializeComponent();
        _codigoDetectado = codigoDetectado;
        BarcodeView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            TryHarder = true,
            Multiple = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Interlocked.Exchange(ref _procesando, 0);
        BarcodeView.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        BarcodeView.IsDetecting = false;
        BarcodeView.IsTorchOn = false;
        base.OnDisappearing();
    }

    private void BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var contenido = e.Results.FirstOrDefault()?.Value;
        if (!CodigoSalaParser.TryParse(contenido, out var codigo)) return;
        if (Interlocked.Exchange(ref _procesando, 1) == 1) return;

        BarcodeView.IsDetecting = false;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _codigoDetectado(codigo);
            await Navigation.PopAsync();
        });
    }

    private void Linterna_Clicked(object? sender, EventArgs e) =>
        BarcodeView.IsTorchOn = !BarcodeView.IsTorchOn;

    private async void Cancelar_Clicked(object? sender, EventArgs e) =>
        await Navigation.PopAsync();
}
