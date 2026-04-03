using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Shells;

public partial class LocationAdminShell : Shell
{
    public LocationAdminShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(LocationAdminSlotsPage), typeof(LocationAdminSlotsPage));
        //Routing.RegisterRoute(nameof(LocationAdminManualArrivalPage), typeof(LocationAdminManualArrivalPage));
        Routing.RegisterRoute(nameof(LocationAdminSlotDetailsPage), typeof(LocationAdminSlotDetailsPage));
        Routing.RegisterRoute(nameof(AdminQrScannerPage), typeof(AdminQrScannerPage));
        Routing.RegisterRoute(nameof(ParkingHistoryPage), typeof(ParkingHistoryPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        LocationAdminDashboardContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<LocationAdminDashboardPage>());

        ProfileContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<ProfilePage>());
    }
}