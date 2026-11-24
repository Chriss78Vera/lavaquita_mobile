using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vInicio : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService _realtime;
    private readonly PartidaLocalStore _localStore;
    private CancellationTokenSource? _healthCheck;
    private bool _servicioDisponible;

    public vInicio(VaquitaApi api, AppSession session, SalaRealtimeService realtime, PartidaLocalStore localStore)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        _localStore = localStore;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _realtime.DesconectarAsync();
        _session.Reiniciar();
        Connectivity.Current.ConnectivityChanged += Connectivity_Changed;
        await ActualizarOpcionesDeConexionAsync();
    }

    protected override void OnDisappearing()
    {
        _healthCheck?.Cancel();
        Connectivity.Current.ConnectivityChanged -= Connectivity_Changed;
        base.OnDisappearing();
    }

    private void Connectivity_Changed(object? sender, ConnectivityChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(async () => await ActualizarOpcionesDeConexionAsync());

    private async Task ActualizarOpcionesDeConexionAsync()
    {
        _healthCheck?.Cancel();
        _healthCheck = new CancellationTokenSource();
        var token = _healthCheck.Token;
        var hayInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        _servicioDisponible = hayInternet && await _api.HealthCheckAsync(token);
        if (token.IsCancellationRequested) return;

        OnlineOptions.IsVisible = _servicioDisponible;
        OfflineOptions.IsVisible = !_servicioDisponible;
        ServiceStatusPanel.IsVisible = !_servicioDisponible;
        ContinuarGuardadaButton.IsVisible = await _localStore.ExisteAsync();
    }

    private async void CrearSala_Clicked(object? sender, EventArgs e)
    {
        if (!await VerificarInternetAsync()) return;
        await Navigation.PushAsync(new vCrearSala(_api, _session, _realtime));
    }

    private async void Unirse_Clicked(object? sender, EventArgs e)
    {
        if (!await VerificarInternetAsync()) return;
        await Navigation.PushAsync(new vUnirseSala(_api, _session, _realtime));
    }

    private async void ModoRapido_Clicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new vModoRapido(_api, _session, _localStore));

    private async void ContinuarGuardada_Clicked(object? sender, EventArgs e)
    {
        if (!await _localStore.RestaurarAsync(_session))
        {
            ContinuarGuardadaButton.IsVisible = false;
            await DisplayAlertAsync("Sin partida guardada", "Arma una nueva vaquita rápida.", "Aceptar");
            return;
        }
        await Navigation.PushAsync(new vJuegos(_api, _session, null, _localStore));
    }

    private async Task<bool> VerificarInternetAsync()
    {
        await ActualizarOpcionesDeConexionAsync();
        if (_servicioDisponible) return true;

        await DisplayAlertAsync(
            "Salas en pausa",
            "El servidor todavía anda de paseo. Puedes seguir de una con el modo rápido.",
            "Aceptar");
        return false;
    }
}
