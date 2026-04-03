using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Shells;

public partial class UserShell : Shell
{
    public UserShell()
    {
        InitializeComponent();

        UserDashboardContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<UserDashboardPage>());

        ProfileContent.ContentTemplate =
            new DataTemplate(() => App.Services.GetRequiredService<ProfilePage>());

        Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        Routing.RegisterRoute(nameof(NavigationMapPage), typeof(NavigationMapPage));
        Routing.RegisterRoute(nameof(ParkingSlotsPage), typeof(ParkingSlotsPage));
        Routing.RegisterRoute(nameof(ArrivalPage), typeof(ArrivalPage));
        Routing.RegisterRoute(nameof(MyActiveParkingPage), typeof(MyActiveParkingPage));
        Routing.RegisterRoute(nameof(ParkingHistoryPage), typeof(ParkingHistoryPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}