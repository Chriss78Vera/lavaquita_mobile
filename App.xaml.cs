using LaVaquita.Services;
using LaVaquita.Views;
using LaVaquita.Models;

namespace LaVaquita;

public partial class App : Application
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;
    private readonly SalaRealtimeService _realtime;
    private readonly PartidaLocalStore _localStore;

    public App(VaquitaApi api, AppSession session, SalaRealtimeService realtime, PartidaLocalStore localStore)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        _localStore = localStore;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new vInicio(_api, _session, _realtime, _localStore))
        {
            BarBackgroundColor = Color.FromArgb("#F7F7F2"),
            BarTextColor = Color.FromArgb("#18211A")
        });
}
