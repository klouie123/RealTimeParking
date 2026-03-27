using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class UserDashboardPage : ContentPage
{
    private readonly NavigationStateService _navigationState;
    public UserDashboardPage()
    {
        InitializeComponent();

        _navigationState = App.Services.GetService<NavigationStateService>();

        var username = Preferences.Get("username", "User");

        if (UsernameLabel != null)
            UsernameLabel.Text = $"Logged in as: {username}";

        if (FlyoutUsernameLabel != null)
            FlyoutUsernameLabel.Text = $"Username: {username}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_navigationState != null && _navigationState.IsNavigating && !_navigationState.HasArrived)
        {
            NavigationBanner.IsVisible = true;
            NavigationBannerLabel.Text =
                $"{_navigationState.DestinationName} • {_navigationState.RemainingDistanceKm:F2} km • ETA {Math.Ceiling(_navigationState.EtaMinutes)} min";
        }
        else
        {
            NavigationBanner.IsVisible = false;
        }
    }

    private async void OpenNavigationBanner_Clicked(object sender, EventArgs e)
    {
        if (_navigationState == null || !_navigationState.IsNavigating)
            return;

        string name = Uri.EscapeDataString(_navigationState.DestinationName ?? "Destination");

        await Shell.Current.GoToAsync(
            $"{nameof(NavigationMapPage)}" +
            $"?destLat={_navigationState.DestinationLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&destLng={_navigationState.DestinationLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&destName={name}");
    }

    private async void QuickSearch_Clicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission", "Location permission denied", "OK");
                return;
            }

            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location == null)
            {
                location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));
            }

            if (location == null)
            {
                await DisplayAlert("Error", "Cannot get location (GPS OFF?)", "OK");
                return;
            }

            await Shell.Current.GoToAsync(
                $"{nameof(MapPage)}?lat={location.Latitude}&lng={location.Longitude}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void InputAddress_Clicked(object sender, EventArgs e)
    {
        try
        {
            string address = await DisplayPromptAsync("Search Address", "Enter location:");
            if (string.IsNullOrWhiteSpace(address))
                return;

            var locations = await Geocoding.GetLocationsAsync(address);
            var location = locations?.FirstOrDefault();

            if (location == null)
            {
                await DisplayAlert("Error", "Location not found", "OK");
                return;
            }

            await Shell.Current.GoToAsync(
                $"{nameof(MapPage)}?lat={location.Latitude}&lng={location.Longitude}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void HamburgerButton_Clicked(object sender, EventArgs e)
    {
        if (FlyoutPanel.IsVisible)
        {
            await FlyoutPanel.TranslateTo(250, 0, 200);
            FlyoutPanel.IsVisible = false;
        }
        else
        {
            FlyoutPanel.IsVisible = true;
            await FlyoutPanel.TranslateTo(0, 0, 200);
        }
    }

    private void LogoutButton_Clicked(object sender, EventArgs e)
    {
        Preferences.Remove("jwt_token");
        Preferences.Remove("user_role");
        Preferences.Remove("username");

        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }
}