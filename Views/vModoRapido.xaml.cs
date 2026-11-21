using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vModoRapido : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;

    public vModoRapido(VaquitaApi api, AppSession session)
    {
        InitializeComponent(); _api = api; _session = session;
    }

    private async void Continuar_Clicked(object? sender, EventArgs e)
    {
        var nombres = NombresEntry.Text?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList() ?? [];
        if (nombres.Count < 2 || !MoneyFormat.TryParse(TotalEntry.Text, out var total) || total <= 0)
        {
            await DisplayAlertAsync("Datos incompletos", "Ingresa entre 2 y 10 nombres y un total mayor que cero.", "Aceptar"); return;
        }
        _session.Reiniciar();
        _session.Cuenta.Total = total;
        _session.Cuenta.Moneda = "USD";
        _session.Participantes = nombres.Select((nombre, index) => new Participante { Id = $"local-{index}", Nombre = nombre }).ToList();
        await Navigation.PushAsync(new vJuegos(_api, _session));
    }
}
