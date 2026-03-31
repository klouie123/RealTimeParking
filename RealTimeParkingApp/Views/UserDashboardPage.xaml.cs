using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class UserDashboardPage : ContentPage
{
    private readonly NavigationStateService _navigationState;
    private readonly ParkingService _parkingService;

    public UserDashboardPage()
    {
        InitializeComponent();

        _navigationState = App.Services.GetService<NavigationStateService>();
        _parkingService = App.Services.GetService<ParkingService>();

        var username = Preferences.Get("username", "User");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadParkingLocationsAsync();

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

    private async Task LoadParkingLocationsAsync()
    {
        try
        {
            var locations = await _parkingService.GetParkingLocationsAsync();

            if (locations == null)
            {
                ParkingLocationsCollectionView.ItemsSource = null;
                return;
            }

            ParkingLocationsCollectionView.ItemsSource = locations;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void ViewSlots_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ParkingLocation location)
            return;

        string encodedName = Uri.EscapeDataString(location.Name ?? "Parking");

        await Shell.Current.GoToAsync(
            $"{nameof(ParkingSlotsPage)}?parkingLocationId={location.Id}&parkingLocationName={encodedName}");
    }

    private async void OpenActiveReservation_Clicked(object sender, EventArgs e)
    {
        var userId = Preferences.Get("user_id", 0);

        if (userId == 0)
        {
            await DisplayAlert("Error", "User not found.", "OK");
            return;
        }

        var reservation = await _parkingService.GetActiveReservationAsync(userId);

        if (reservation == null)
        {
            await DisplayAlert("Info", "You have no active reservation.", "OK");
            return;
        }

        _navigationState.StartNavigation(
            reservation.ParkingLocationName,
            reservation.Latitude,
            reservation.Longitude);

        string name = Uri.EscapeDataString(reservation.ParkingLocationName);

        await Shell.Current.GoToAsync(
            $"{nameof(NavigationMapPage)}" +
            $"?destLat={reservation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&destLng={reservation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&destName={name}");
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
}