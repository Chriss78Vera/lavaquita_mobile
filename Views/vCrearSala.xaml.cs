using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vCrearSala : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService _realtime;

    public vCrearSala(VaquitaApi api, AppSession session, SalaRealtimeService realtime)
    {
        InitializeComponent(); _api = api; _session = session; _realtime = realtime;
        var avatars = AvatarOption.Todos; AvatarCollection.ItemsSource = avatars; AvatarCollection.SelectedItem = avatars[0];
    }

    private async void Crear_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NombreEntry.Text)) { await DisplayAlertAsync("Falta tu nombre", "Escribe cómo aparecerás en la sala.", "Aceptar"); return; }
        CrearButton.IsEnabled = false; Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            var avatar = (AvatarOption)(AvatarCollection.SelectedItem ?? AvatarOption.Todos[0]);
            var credentials = await _api.CrearSalaAsync(NombreEntry.Text.Trim(), avatar.Id);
            _session.Reiniciar(); _session.EsCompartida = _session.EsAnfitrion = true;
            _session.CodigoSala = credentials.Codigo; _session.HostToken = credentials.HostToken;
            _session.ParticipantToken = credentials.ParticipantToken; _session.NombreActual = NombreEntry.Text.Trim();
            await Navigation.PushAsync(new vSala(_api, _session, _realtime));
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo crear", error.Message, "Aceptar"); }
        finally { CrearButton.IsEnabled = true; Cargando.IsVisible = Cargando.IsRunning = false; }
    }
}
