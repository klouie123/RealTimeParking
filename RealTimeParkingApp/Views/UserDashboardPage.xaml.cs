using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

public partial class UserDashboardPage : ContentPage
{
    private readonly NavigationStateService _navigationState;
    private ActiveReservation? _activeReservation;
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
        UpdateNavigationBanner();
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
            $"{nameof(ParkingSlotsPage)}" +
            $"?parkingLocationId={location.Id}" +
            $"&parkingLocationName={Uri.EscapeDataString(location.Name ?? "Parking")}" +
            $"&lat={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&lng={location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
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
            ReservedPanel.IsVisible = false;
            return;
        }

        _activeReservation = reservation;

        ReservedLocationLabel.Text = $"Location: {reservation.ParkingLocationName}";
        ReservedSlotLabel.Text = $"Slot: {reservation.SlotCode}";
        ReservedStatusLabel.Text = $"Status: {reservation.Status}";
        ReservedExpiryLabel.Text = reservation.ExpiresAt.HasValue
             ? $"Expires At: {DateTime.SpecifyKind(reservation.ExpiresAt.Value, DateTimeKind.Utc).ToLocalTime():MMM dd, yyyy hh:mm tt}"
             : "Expires At: Not set";

        ReservedPanel.IsVisible = true;
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

    private async void NavigateReserved_Clicked(object sender, EventArgs e)
    {
        if (_activeReservation == null)
        {
            await DisplayAlert("Info", "No active reservation found.", "OK");
            return;
        }

        _navigationState.StartNavigation(
            _activeReservation.ParkingLocationName,
            _activeReservation.Latitude,
            _activeReservation.Longitude);

        UpdateNavigationBanner();

        string name = Uri.EscapeDataString(_activeReservation.ParkingLocationName);

        await Shell.Current.GoToAsync(
            $"{nameof(NavigationMapPage)}" +
            $"?destLat={_activeReservation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&destLng={_activeReservation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&destName={name}");
    }

    private async void CancelReserved_Clicked(object sender, EventArgs e)
    {
        if (_activeReservation == null)
        {
            await DisplayAlert("Info", "No active reservation found.", "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            "Cancel Reservation",
            "Are you sure you want to cancel this reservation?",
            "Yes",
            "No");

        if (!confirm)
            return;

        var success = await _parkingService.CancelReservationAsync(_activeReservation.Id);

        if (!success)
        {
            await DisplayAlert("Error", "Failed to cancel reservation.", "OK");
            return;
        }

        _navigationState.StopNavigation();
        _activeReservation = null;
        ReservedPanel.IsVisible = false;

        UpdateNavigationBanner();

        await DisplayAlert("Success", "Reservation cancelled successfully.", "OK");
        await LoadParkingLocationsAsync();
    }

    private void UpdateNavigationBanner()
    {
        if (_navigationState != null && _navigationState.IsNavigating && !_navigationState.HasArrived)
        {
            NavigationBanner.IsVisible = true;
            NavigationBannerLabel.Text =
                $"{_navigationState.DestinationName} • {_navigationState.RemainingDistanceKm:F2} km • ETA {Math.Ceiling(_navigationState.EtaMinutes)} min";
        }
        else
        {
            NavigationBanner.IsVisible = false;
            NavigationBannerLabel.Text = "Tap to return to navigation";
        }
    }

    private void CloseReservedPanel_Clicked(object sender, EventArgs e)
    {
        ReservedPanel.IsVisible = false;
    }
}