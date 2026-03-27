using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Shells;

public partial class UserShell : Shell
{
	public UserShell()
	{
		InitializeComponent();

        Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        Routing.RegisterRoute(nameof(NavigationMapPage), typeof(NavigationMapPage));
    }
}