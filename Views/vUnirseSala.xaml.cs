using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;
using ZXing.Net.Maui;

namespace LaVaquita.Views;

public partial class vUnirseSala : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService _realtime;

    public vUnirseSala(VaquitaApi api, AppSession session, SalaRealtimeService realtime)
    {
        InitializeComponent(); _api = api; _session = session; _realtime = realtime;
        var avatars = AvatarOption.Todos; AvatarCollection.ItemsSource = avatars; AvatarCollection.SelectedItem = avatars[0];
    }

    private void Codigo_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var upper = e.NewTextValue?.ToUpperInvariant(); if (CodigoEntry.Text != upper) CodigoEntry.Text = upper;
    }

    private async void EscanearQr_Clicked(object? sender, EventArgs e)
    {
        if (!BarcodeScanning.IsSupported)
        {
            await DisplayAlertAsync("Cámara no disponible", "En este dispositivo puedes ingresar el código manualmente.", "Aceptar");
            return;
        }

        var permission = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
            permission = await Permissions.RequestAsync<Permissions.Camera>();
        if (permission != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Permiso rechazado", "Puedes escribir el código de la sala manualmente.", "Aceptar");
            return;
        }

        await Navigation.PushAsync(new vEscanearQr(codigo => CodigoEntry.Text = codigo));
    }

    private async void Entrar_Clicked(object? sender, EventArgs e)
    {
        if (!CodigoSalaParser.TryParse(CodigoEntry.Text, out var codigo) || string.IsNullOrWhiteSpace(NombreEntry.Text))
        { await DisplayAlertAsync("Datos incompletos", "Escribe el código y tu nombre.", "Aceptar"); return; }
        EntrarButton.IsEnabled = false; Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            var avatar = (AvatarOption)(AvatarCollection.SelectedItem ?? AvatarOption.Todos[0]);
            var participantToken = await _api.UnirseAsync(codigo, NombreEntry.Text.Trim(), avatar.Id);
            _session.Reiniciar(); _session.EsCompartida = true; _session.EsAnfitrion = false;
            _session.CodigoSala = codigo; _session.ParticipantToken = participantToken;
            _session.NombreActual = NombreEntry.Text.Trim();
            await Navigation.PushAsync(new vSala(_api, _session, _realtime));
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo entrar", error.Message, "Aceptar"); }
        finally { EntrarButton.IsEnabled = true; Cargando.IsVisible = Cargando.IsRunning = false; }
    }
}
