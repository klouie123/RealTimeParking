using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
#if ANDROID
using Android.Gms.Maps;
#endif

namespace RealTimeParkingApp.Views;

public partial class UserDashboardPage : ContentPage
{
    private readonly NavigationStateService _navigationState;
    private ActiveReservation? _activeReservation;
    private readonly ParkingService _parkingService;
    private readonly ApiService _apiService;
    private readonly LocationService _locationService;

    private ParkingLocation? _selectedParkingLocation;
    private Pin? _selectedParkingPin;
    private bool _mapInitialized;

    

    public UserDashboardPage()
    {
        InitializeComponent();

        _apiService = App.Services.GetRequiredService<ApiService>();
        _navigationState = App.Services.GetRequiredService<NavigationStateService>();
        _parkingService = App.Services.GetRequiredService<ParkingService>();
        _locationService = App.Services.GetRequiredService<LocationService>();

        PreviewMap.HandlerChanged += PreviewMap_HandlerChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await LoadParkingLocationsAsync();
            await InitializeMapToUserLocationAsync();
            UpdateNavigationBanner();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Crash", ex.ToString(), "OK");
        }
    }

    private async Task InitializeMapToUserLocationAsync()
    {
        try
        {
            if (_mapInitialized)
                return;

            var userLocation = await _locationService.GetCurrentLocationAsync();

            if (userLocation != null)
            {
                PreviewMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        new Location(userLocation.Latitude, userLocation.Longitude),
                        Distance.FromMeters(500)));
            }

            _mapInitialized = true;
        }
        catch
        {
            // safe fallback lang, no alert
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
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private void PreviewMap_HandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (PreviewMap.Handler?.PlatformView is Android.Gms.Maps.MapView mapView)
        {
            mapView.GetMapAsync(new DashboardMapReadyCallback());
        }
#endif
    }

    private void ParkingCard_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (e.Parameter is not ParkingLocation location)
                return;

            ShowLocationPreview(location);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", ex.ToString(), "OK");
            });
        }
    }

    private void ShowLocationPreview(ParkingLocation location)
    {
        _selectedParkingLocation = location;

        SelectedLocationTitleLabel.Text = location.Name ?? "Parking Location";
        SelectedLocationDescriptionLabel.Text = string.IsNullOrWhiteSpace(location.Description)
            ? "No description available."
            : location.Description;

        var position = new Location(location.Latitude, location.Longitude);

        PreviewMap.Pins.Clear();

        _selectedParkingPin = new Pin
        {
            Label = location.Name ?? "Parking",
            Address = location.Description ?? "",
            Location = position
        };

        PreviewMap.Pins.Add(_selectedParkingPin);

        PreviewMap.MoveToRegion(
            MapSpan.FromCenterAndRadius(position, Distance.FromMeters(300)));
    }

    private async void ViewSlots_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn || btn.CommandParameter is not ParkingLocation location)
                return;

            await OpenParkingSlots(location);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private async Task OpenParkingSlots(ParkingLocation location)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(ParkingSlotsPage)}" +
            $"?parkingLocationId={location.Id}" +
            $"&parkingLocationName={Uri.EscapeDataString(location.Name ?? "Parking")}" +
            $"&lat={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&lng={location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    private async void OpenNavigationBanner_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!_navigationState.IsNavigating)
                return;

            string name = Uri.EscapeDataString(_navigationState.DestinationName ?? "Destination");

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_navigationState.DestinationLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&destLng={_navigationState.DestinationLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&destName={name}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private async void ReservedFloatingButton_Clicked(object sender, EventArgs e)
    {
        try
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

            _activeReservation = reservation;

            ReservedLocationLabel.Text = $"Location: {reservation.ParkingLocationName}";
            ReservedSlotLabel.Text = $"Slot: {reservation.SlotCode}";
            ReservedStatusLabel.Text = $"Status: {reservation.Status}";
            ReservedExpiryLabel.Text = reservation.ExpiresAt.HasValue
                ? $"Expires At: {DateTime.SpecifyKind(reservation.ExpiresAt.Value, DateTimeKind.Utc).ToLocalTime():MMM dd, yyyy hh:mm tt}"
                : "Expires At: Not set";

            await ShowReservedPanelAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private async Task ShowReservedPanelAsync()
    {
        ReservedOverlay.IsVisible = true;
        ReservedPanel.TranslationY = 320;
        await ReservedPanel.TranslateTo(0, 0, 220, Easing.CubicOut);
    }

    private async Task HideReservedPanelAsync()
    {
        await ReservedPanel.TranslateTo(0, 320, 180, Easing.CubicIn);
        ReservedOverlay.IsVisible = false;
    }

    private async void ReservedOverlay_Tapped(object sender, TappedEventArgs e)
    {
        await HideReservedPanelAsync();
    }

    private async void NavigateReserved_Clicked(object sender, EventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private async void CancelReserved_Clicked(object sender, EventArgs e)
    {
        try
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
            await HideReservedPanelAsync();

            UpdateNavigationBanner();

            await DisplayAlert("Success", "Reservation cancelled successfully.", "OK");
            await LoadParkingLocationsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private void UpdateNavigationBanner()
    {
        if (_navigationState.IsNavigating && !_navigationState.HasArrived)
        {
            NavigationBanner.IsVisible = true;
            FloatingNavigationButton.IsVisible = true;

            NavigationBannerLabel.Text =
                $"{_navigationState.DestinationName} • {_navigationState.RemainingDistanceKm:F2} km • ETA {Math.Ceiling(_navigationState.EtaMinutes)} min";
        }
        else
        {
            NavigationBanner.IsVisible = false;
            FloatingNavigationButton.IsVisible = false;
            NavigationBannerLabel.Text = "Tap to return to navigation";
        }
    }

    private async void FloatingNavigationButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!_navigationState.IsNavigating)
            {
                await DisplayAlert("Info", "No active navigation yet.", "OK");
                return;
            }

            string name = Uri.EscapeDataString(_navigationState.DestinationName ?? "Destination");

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_navigationState.DestinationLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&destLng={_navigationState.DestinationLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&destName={name}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private async void CloseReservedPanel_Clicked(object sender, EventArgs e)
    {
        await HideReservedPanelAsync();
    }

#if ANDROID
    private sealed class DashboardMapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        public void OnMapReady(GoogleMap googleMap)
        {
            var ui = googleMap.UiSettings;

            ui.ZoomControlsEnabled = false;
            ui.MyLocationButtonEnabled = false;
            ui.ScrollGesturesEnabled = false;
            ui.ZoomGesturesEnabled = false;
            ui.RotateGesturesEnabled = false;
            ui.TiltGesturesEnabled = false;
            ui.MapToolbarEnabled = false;
        }
    }
#endif
}

