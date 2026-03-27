using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using RealTimeParkingApp.Views;

namespace RealTimeParkingApp.Views;

public partial class UserDashboardPage : ContentPage
{
    public UserDashboardPage()
    {
        InitializeComponent();

        var username = Preferences.Get("username", "User");
        if (UsernameLabel != null)
            UsernameLabel.Text = $"Logged in as: {username}";

        if (FlyoutUsernameLabel != null)
            FlyoutUsernameLabel.Text = $"Username: {username}";
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
        string address = await DisplayPromptAsync("Search Address", "Enter location:");
        if (string.IsNullOrWhiteSpace(address)) return;

        var locations = await Geocoding.GetLocationsAsync(address);
        var location = locations?.FirstOrDefault();
        if (location == null)
        {
            await DisplayAlert("Error", "Location not found", "OK");
            return;
        }

        var mapPage = App.Services.GetService<MapPage>();
        if (mapPage != null)
        {
            await Navigation.PushAsync(mapPage);

            // set location AFTER navigation
            await mapPage.InitializeAsync(location.Latitude, location.Longitude);
        }
        else
        {
            await DisplayAlert("Error", "MapPage not available", "OK");
        }
    }

    private async void HamburgerButton_Clicked(object sender, EventArgs e)
    {
        if (FlyoutPanel.IsVisible)
        {
            // Hide flyout
            await FlyoutPanel.TranslateTo(250, 0, 200);
            FlyoutPanel.IsVisible = false;
        }
        else
        {
            // Show flyout
            FlyoutPanel.IsVisible = true;
            await FlyoutPanel.TranslateTo(0, 0, 200);
        }
    }

    private void LogoutButton_Clicked(object sender, EventArgs e)
    {
        Preferences.Remove("jwt_token");
        Preferences.Remove("user_role");
        Preferences.Remove("username");

        Application.Current.MainPage = new LoginPage();
    }
}