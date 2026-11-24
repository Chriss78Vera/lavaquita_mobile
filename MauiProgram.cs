using LaVaquita.Services;
using LaVaquita.Models;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace LaVaquita;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseBarcodeReader().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

        // Android Emulator accede al equipo anfitrión mediante 10.0.2.2.
        string apiUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:3000/"
            : "https://lcpf9c62-3000.use2.devtunnels.ms/";
        var apiUri = new Uri(apiUrl);
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = apiUri,
            Timeout = TimeSpan.FromSeconds(35)
        });
        builder.Services.AddSingleton<VaquitaApi>();
        builder.Services.AddSingleton(new SalaRealtimeService(apiUri));
        builder.Services.AddSingleton<AppSession>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
