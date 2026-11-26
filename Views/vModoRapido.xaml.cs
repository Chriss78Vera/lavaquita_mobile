using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using LaVaquita.Models;
using LaVaquita.Services;
using LaVaquita.Utils;

namespace LaVaquita.Views;

public partial class vModoRapido : ContentPage
{
    public const double AltoFilaParticipante = 48;
    private const int FilasVisibles = 2;
    private const int MaximoParticipantes = 10;
    private static readonly Regex FormatoNombre = new(@"\A\p{L}[\p{L}\p{M}0-9]*(?:[ '’\-][\p{L}0-9][\p{L}\p{M}0-9]*)*\z");
    private readonly ObservableCollection<Participante> _participantes = [];
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly PartidaLocalStore _localStore;

    public vModoRapido(VaquitaApi api, AppSession session, PartidaLocalStore localStore)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _localStore = localStore;
        BindableLayout.SetItemsSource(ListaParticipantes, _participantes);
        ActualizarParticipantes();
    }

    private void AgregarNombre(object? sender, EventArgs e) => IntentarAgregarNombre();

    private bool IntentarAgregarNombre()
    {
        var nombre = Regex.Replace((NombreEntry.Text ?? "").Trim(), " +", " ").Normalize();
        if (nombre.Length > 30 || !FormatoNombre.IsMatch(nombre))
        {
            MostrarAvisoNombres("Escribe un nombre de hasta 30 caracteres que empiece con una letra. Puedes usar números, espacios, guiones y apóstrofes.");
            return false;
        }

        if (_participantes.Any(persona => string.Equals(persona.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
        {
            MostrarAvisoNombres("Ese nombre ya está en la lista. Usa otro apodo o añade un número.");
            return false;
        }

        if (_participantes.Count >= MaximoParticipantes)
        {
            MostrarAvisoNombres($"Puedes añadir hasta {MaximoParticipantes} participantes.");
            return false;
        }

        _participantes.Add(new Participante { Id = $"local-{Guid.NewGuid():N}", Nombre = nombre });
        NombreEntry.Text = "";
        ActualizarParticipantes();
        return true;
    }

    private void QuitarNombre(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: Participante persona })
        {
            _participantes.Remove(persona);
            ActualizarParticipantes();
        }
    }

    private void ActualizarParticipantes()
    {
        var cantidad = _participantes.Count;
        ConteoParticipantesLabel.Text = $"{cantidad} de {MaximoParticipantes} · mínimo 2";
        NombresAvisoLabel.IsVisible = false;
        ScrollParticipantes.HeightRequest = Math.Min(cantidad, FilasVisibles) * AltoFilaParticipante;
        ScrollParticipantes.IsVisible = cantidad > 0;
        var tieneMasFilas = cantidad > FilasVisibles;
        ScrollParticipantes.Orientation = tieneMasFilas ? ScrollOrientation.Vertical : ScrollOrientation.Neither;
        ScrollParticipantes.VerticalScrollBarVisibility = tieneMasFilas ? ScrollBarVisibility.Always : ScrollBarVisibility.Never;
    }

    private void MostrarAvisoNombres(string mensaje)
    {
        NombresAvisoLabel.Text = mensaje;
        NombresAvisoLabel.IsVisible = true;
    }

    private async void Continuar_Clicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(NombreEntry.Text) && !IntentarAgregarNombre())
            return;

        if (_participantes.Count < 2)
        {
            MostrarAvisoNombres("Añade al menos dos participantes para continuar.");
            return;
        }

        if (!MoneyFormat.TryParse(TotalEntry.Text, out var total))
        {
            await DisplayAlertAsync("Revisa el total", MoneyFormat.MensajeMontoInvalido, "Aceptar");
            return;
        }

        _session.Reiniciar();
        _session.Cuenta.Total = total;
        _session.Cuenta.Moneda = "USD";
        _session.Participantes = _participantes.ToList();
        await _localStore.GuardarAsync(_session);
        await Navigation.PushAsync(new vJuegos(_api, _session, null, _localStore));
    }
}
