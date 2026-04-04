using Microsoft.Extensions.Logging;
using RealTimeParkingApp.Services;
using RealTimeParkingApp.Shells;
using RealTimeParkingApp.ViewModels;
using RealTimeParkingApp.Views;
using ZXing.Net.Maui.Controls;

namespace RealTimeParkingApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<ParkingService>();
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<NavigationStateService>();

            builder.Services.AddTransient<MapViewModel>();

            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<ParkingHistoryPage>();
            builder.Services.AddTransient<SettingsPage>();

            builder.Services.AddTransient<StartupPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<ProfilePage>();

            builder.Services.AddTransient<UserDashboardPage>();
            //builder.Services.AddTransient<ArrivalPage>();
            builder.Services.AddTransient<ParkingSlotsPage>();
            builder.Services.AddTransient<NavigationMapPage>();

            builder.Services.AddTransient<SuperAdminDashboardPage>();

            builder.Services.AddTransient<LocationAdminDashboardPage>();
            builder.Services.AddTransient<LocationAdminSlotsPage>();
            //builder.Services.AddTransient<LocationAdminManualArrivalPage>();
            builder.Services.AddTransient<MyActiveParkingPage>();
            builder.Services.AddTransient<LocationAdminSlotDetailsPage>();
            builder.Services.AddTransient<AdminQrScannerPage>();
            builder.Services.AddTransient<LocationAdminHistoryPage>();

            builder.Services.AddTransient<UserShell>();
            builder.Services.AddTransient<SuperAdminShell>();
            builder.Services.AddTransient<LocationAdminShell>();

            return builder.Build();
        }
    }
} 