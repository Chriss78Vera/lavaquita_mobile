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
        Connectivity.Current.ConnectivityChanged += Connectivity_Changed;
        ActualizarOpcionesDeConexion();
    }

    protected override void OnDisappearing()
    {
        Connectivity.Current.ConnectivityChanged -= Connectivity_Changed;
        base.OnDisappearing();
    }

    private void Connectivity_Changed(object? sender, ConnectivityChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(ActualizarOpcionesDeConexion);

    private void ActualizarOpcionesDeConexion()
    {
        bool hayInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        OnlineOptions.IsVisible = hayInternet;
        OfflineOptions.IsVisible = !hayInternet;
    }

    private async void CrearSala_Clicked(object? sender, EventArgs e)
    {
        if (!await VerificarInternetAsync()) return;
        await Navigation.PushAsync(new vCrearSala(_api, _session));
    }

    private async void Unirse_Clicked(object? sender, EventArgs e)
    {
        if (!await VerificarInternetAsync()) return;
        await Navigation.PushAsync(new vUnirseSala(_api, _session));
    }

    private async void ModoRapido_Clicked(object? sender, EventArgs e) => await Navigation.PushAsync(new vModoRapido(_api, _session));

    private async Task<bool> VerificarInternetAsync()
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet) return true;

        ActualizarOpcionesDeConexion();
        await DisplayAlertAsync(
            "Sin conexión",
            "Crear o unirse a una sala requiere internet. Puedes continuar en modo rápido.",
            "Aceptar");
        return false;
    }
}
