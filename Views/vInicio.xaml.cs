using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vInicio : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;

    public vInicio(VaquitaApi api, AppSession session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _session.Reiniciar();
    }

    private async void CrearSala_Clicked(object? sender, EventArgs e) => await Navigation.PushAsync(new vCrearSala(_api, _session));
    private async void Unirse_Clicked(object? sender, EventArgs e) => await Navigation.PushAsync(new vUnirseSala(_api, _session));
    private async void ModoRapido_Clicked(object? sender, EventArgs e) => await Navigation.PushAsync(new vModoRapido(_api, _session));
}
