using LaVaquita.Services;
using LaVaquita.Views;
using LaVaquita.Models;

namespace LaVaquita;

public partial class App : Application
{
    private readonly VaquitaApi _api;
    private readonly AppSession _session;

    public App(VaquitaApi api, AppSession session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new NavigationPage(new vInicio(_api, _session))
        {
            BarBackgroundColor = Color.FromArgb("#F7F7F2"),
            BarTextColor = Color.FromArgb("#18211A")
        });
}
