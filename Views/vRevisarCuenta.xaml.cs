using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vRevisarCuenta : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly Cuenta _cuenta;

    public vRevisarCuenta(VaquitaApi api, AppSession session, Cuenta cuenta)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _cuenta = cuenta;
        TotalEntry.Text = cuenta.Total.ToString("F2", MoneyFormat.UnitedStates);
        ConfianzaLabel.Text = $"Confianza de lectura: {cuenta.ConfianzaOcr:P0}";
    }

    private async void Confirmar_Clicked(object? sender, EventArgs e)
    {
        if (!MoneyFormat.TryParse(TotalEntry.Text, out var total))
        {
            await DisplayAlertAsync("Monto inválido", MoneyFormat.MensajeMontoInvalido, "Aceptar");
            return;
        }

        ConfirmarButton.IsEnabled = false;
        Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            _cuenta.Total = total;
            _cuenta.Moneda = "USD";
            await _api.ConfirmarCuentaAsync(_session.CodigoSala, _session.HostToken, _cuenta);
            _session.Cuenta = _cuenta;
            await Navigation.PopAsync();
        }
        catch (Exception error)
        {
            await DisplayAlertAsync("No se pudo guardar", error.Message, "Aceptar");
        }
        finally
        {
            ConfirmarButton.IsEnabled = true;
            Cargando.IsVisible = Cargando.IsRunning = false;
        }
    }

    private async void Cancelar_Clicked(object? sender, EventArgs e) => await Navigation.PopAsync();
}
