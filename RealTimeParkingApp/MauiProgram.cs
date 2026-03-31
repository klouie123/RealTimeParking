using MauiIcons.Fluent.Filled;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.ViewModels;
using RealTimeParkingApp.Views;

namespace RealTimeParkingApp
{
    public static class MauiProgram
    {

        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
            // Initialises the .Net Maui Icons - Fluent
                .UseFluentFilledMauiIcons();


#if DEBUG
            builder.Configuration.AddUserSecrets<App>();
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<ParkingService>();
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<MapViewModel>();
            builder.Services.AddSingleton<MapPage>();
            builder.Services.AddSingleton<NavigationStateService>();
            builder.Services.AddSingleton<NavigationMapPage>();

            var app = builder.Build();

            ServiceProvider = app.Services;

            return app;
        }
    }
}
