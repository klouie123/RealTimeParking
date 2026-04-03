using System.Globalization;
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
    private readonly ParkingService _parkingService;
    private readonly ApiService _apiService;
    private readonly LocationService _locationService;

    private ActiveParkingModel? _activeParking;
    private ParkingLocation? _selectedParkingLocation;
    private Pin? _selectedParkingPin;
    private bool _mapInitialized;

    private CancellationTokenSource? _refreshCts;
    private bool _isBusy;

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
            await RefreshDashboardAsync();
            StartAutoRefresh();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Crash", ex.ToString(), "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAutoRefresh();
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        _refreshCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!_refreshCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), _refreshCts.Token);

                    if (_refreshCts.Token.IsCancellationRequested || _isBusy)
                        continue;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (!_isBusy)
                            await RefreshDashboardAsync(false);
                    });
                }
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    private void StopAutoRefresh()
    {
        if (_refreshCts != null)
        {
            _refreshCts.Cancel();
            _refreshCts.Dispose();
            _refreshCts = null;
        }
    }

    private async Task RefreshDashboardAsync(bool showError = true)
    {
        try
        {
            await LoadParkingLocationsAsync(showError);
            await InitializeMapToUserLocationAsync();
            await RefreshActiveParkingStateAsync(showError);
            UpdateNavigationBanner();
        }
        catch (Exception ex)
        {
            if (showError)
                await DisplayAlert("Error", ex.Message, "OK");
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
        }
    }

    private async Task LoadParkingLocationsAsync(bool showError = true)
    {
        try
        {
            var locations = await _parkingService.GetParkingLocationsAsync();
            ParkingLocationsCollectionView.ItemsSource = locations ?? new List<ParkingLocation>();
        }
        catch (Exception ex)
        {
            if (showError)
                await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task RefreshActiveParkingStateAsync(bool showError = true)
    {
        try
        {
            _activeParking = await _apiService.GetMyActiveParkingAsync();

            if (_activeParking == null)
            {
                _navigationState.ClearAll();

                ReservedFloatingButton.IsVisible = false;
                ReservedOverlay.IsVisible = false;

                return;
            }

            ReservedFloatingButton.IsVisible = true;

            if (_activeParking.Status == "Occupied")
            {
                _navigationState.ClearAll();
            }
            else if (_activeParking.Status != "Reserved")
            {
                _navigationState.ClearAll();
            }
        }
        catch (Exception ex)
        {
            if (showError)
                await DisplayAlert("Error", ex.Message, "OK");
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
        StopAutoRefresh();

        await Shell.Current.GoToAsync(
            $"{nameof(ParkingSlotsPage)}" +
            $"?parkingLocationId={location.Id}" +
            $"&parkingLocationName={Uri.EscapeDataString(location.Name ?? "Parking")}" +
            $"&lat={location.Latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&lng={location.Longitude.ToString(CultureInfo.InvariantCulture)}");
    }

    private async void OpenNavigationBanner_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!_navigationState.IsNavigating || !_navigationState.HasDestination)
                return;

            string name = Uri.EscapeDataString(_navigationState.DestinationName ?? "Destination");

            StopAutoRefresh();

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_navigationState.DestinationLat.ToString(CultureInfo.InvariantCulture)}" +
                $"&destLng={_navigationState.DestinationLng.ToString(CultureInfo.InvariantCulture)}" +
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
            if (_activeParking == null)
            {
                ReservedFloatingButton.IsVisible = false;
                await DisplayAlert("Info", "No active reservation found.", "OK");
                return;
            }

            StopAutoRefresh();
            await Shell.Current.GoToAsync(nameof(MyActiveParkingPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task ShowReservedPanelAsync()
    {
        if (_activeParking == null)
            return;

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
            if (_activeParking == null)
            {
                await DisplayAlert("Info", "No active reservation found.", "OK");
                return;
            }

            if (_activeParking.Status != "Reserved")
            {
                await DisplayAlert("Info", "Navigation is only available before arrival.", "OK");
                return;
            }

            if (_activeParking.Latitude == 0 || _activeParking.Longitude == 0)
            {
                await DisplayAlert("Info", "Location coordinates are not available.", "OK");
                return;
            }

            _navigationState.StartNavigation(
                _activeParking.ParkingLocationName,
                _activeParking.Latitude,
                _activeParking.Longitude);

            UpdateNavigationBanner();

            string name = Uri.EscapeDataString(_activeParking.ParkingLocationName);

            StopAutoRefresh();

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_activeParking.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                $"&destLng={_activeParking.Longitude.ToString(CultureInfo.InvariantCulture)}" +
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
            if (_activeParking == null)
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

            _isBusy = true;

            var success = await _parkingService.CancelReservationAsync(_activeParking.ReservationId);

            if (!success)
            {
                await DisplayAlert("Error", "Failed to cancel reservation.", "OK");
                return;
            }

            _navigationState.ClearAll();
            _activeParking = null;

            await HideReservedPanelAsync();
            await RefreshDashboardAsync(false);

            await DisplayAlert("Success", "Reservation cancelled successfully.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void UpdateNavigationBanner()
    {
        bool shouldShow =
            _navigationState.HasDestination &&
            _navigationState.IsNavigating &&
            !_navigationState.HasArrived;

        NavigationBanner.IsVisible = shouldShow;
        FloatingNavigationButton.IsVisible = shouldShow;

        if (shouldShow)
        {
            NavigationBannerLabel.Text =
                $"{_navigationState.DestinationName} • {_navigationState.RemainingDistanceKm:F2} km • ETA {Math.Ceiling(_navigationState.EtaMinutes)} min";
        }
        else
        {
            NavigationBannerLabel.Text = "Tap to return to navigation";
        }
    }

    private async void FloatingNavigationButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (!_navigationState.HasDestination || !_navigationState.IsNavigating)
            {
                await DisplayAlert("Info", "No active destination yet.", "OK");
                return;
            }

            string name = Uri.EscapeDataString(_navigationState.DestinationName ?? "Destination");

            StopAutoRefresh();

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={_navigationState.DestinationLat.ToString(CultureInfo.InvariantCulture)}" +
                $"&destLng={_navigationState.DestinationLng.ToString(CultureInfo.InvariantCulture)}" +
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