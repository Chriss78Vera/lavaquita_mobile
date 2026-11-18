using LaVaquita.Models;
using LaVaquita.Services;

namespace LaVaquita.Views;

public partial class vUnirseSala : ContentPage
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;

    public vUnirseSala(VaquitaApi api, AppSession session)
    {
        InitializeComponent(); _api = api; _session = session;
        var avatars = AvatarOption.Todos; AvatarCollection.ItemsSource = avatars; AvatarCollection.SelectedItem = avatars[0];
    }

    private void Codigo_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var upper = e.NewTextValue?.ToUpperInvariant(); if (CodigoEntry.Text != upper) CodigoEntry.Text = upper;
    }

    private async void Entrar_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodigoEntry.Text) || string.IsNullOrWhiteSpace(NombreEntry.Text))
        { await DisplayAlertAsync("Datos incompletos", "Escribe el código y tu nombre.", "Aceptar"); return; }
        EntrarButton.IsEnabled = false; Cargando.IsVisible = Cargando.IsRunning = true;
        try
        {
            var avatar = (AvatarOption)(AvatarCollection.SelectedItem ?? AvatarOption.Todos[0]);
            await _api.UnirseAsync(CodigoEntry.Text.Trim(), NombreEntry.Text.Trim(), avatar.Id);
            _session.Reiniciar(); _session.EsCompartida = true; _session.EsAnfitrion = false;
            _session.CodigoSala = CodigoEntry.Text.Trim(); _session.NombreActual = NombreEntry.Text.Trim();
            await Navigation.PushAsync(new vSala(_api, _session));
        }
        catch (Exception error) { await DisplayAlertAsync("No se pudo entrar", error.Message, "Aceptar"); }
        finally { EntrarButton.IsEnabled = true; Cargando.IsVisible = Cargando.IsRunning = false; }
    }
}
