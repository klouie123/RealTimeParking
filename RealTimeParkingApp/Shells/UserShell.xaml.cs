using MauiIcons.Core;
using MauiIcons.Fluent;
using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Shells;

public partial class UserShell : Shell
{
	public UserShell()
	{
		InitializeComponent();

        _ = new MauiIcon();

        Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        Routing.RegisterRoute(nameof(NavigationMapPage), typeof(NavigationMapPage));
        Routing.RegisterRoute(nameof(ParkingSlotsPage), typeof(ParkingSlotsPage));
    }
}