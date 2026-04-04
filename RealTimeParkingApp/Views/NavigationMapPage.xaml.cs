using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(DestinationLat), "destLat")]
[QueryProperty(nameof(DestinationLng), "destLng")]
[QueryProperty(nameof(DestinationName), "destName")]
[QueryProperty(nameof(ReservationId), "reservationId")]
public partial class NavigationMapPage : ContentPage
{
    private readonly NavigationStateService _navigationState;
    private readonly ApiService _apiService;

    private Polyline? _routeLine;
    private Pin? _userPin;
    private Pin? _destinationPin;

    private bool _started;
    private bool _isUpdating;
    private bool _hasReturnedAfterArrival;
    private int _navigationSessionId;
    private int _reservationId;

    private double _destLat;
    private double _destLng;

    public string DestinationLat { get; set; } = string.Empty;
    public string DestinationLng { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;

    public string ReservationId
    {
        get => _reservationId.ToString();
        set => int.TryParse(value, out _reservationId);
    }

    public NavigationMapPage(NavigationStateService navigationState)
    {
        InitializeComponent();
        _navigationState = navigationState;
        _apiService = App.Services.GetRequiredService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!double.TryParse(DestinationLat, NumberStyles.Any, CultureInfo.InvariantCulture, out _destLat) ||
            !double.TryParse(DestinationLng, NumberStyles.Any, CultureInfo.InvariantCulture, out _destLng))
        {
            await DisplayAlert("Error", "Invalid destination coordinates.", "OK");
            return;
        }

        DestinationLabel.Text = string.IsNullOrWhiteSpace(DestinationName)
            ? "Destination"
            : Uri.UnescapeDataString(DestinationName);

        if (_started)
            return;

        _started = true;
        await StartNavigationAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _started = false;
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("..");
        });

        return true;
    }

    private async Task StartNavigationAsync()
    {
        try
        {
            var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission", "Location permission denied.", "OK");
                return;
            }

            _navigationSessionId++;
            int currentSession = _navigationSessionId;

            _hasReturnedAfterArrival = false;
            ArrivedPanel.IsVisible = false;
            StatusLabel.Text = "Loading route...";
            DistanceLabel.Text = "Distance: --";
            SpeedLabel.Text = "Speed: --";
            EtaLabel.Text = "ETA: --";

            ClearMap();

            await UpdateNavigationAsync();

            Device.StartTimer(TimeSpan.FromSeconds(4), () =>
            {
                if (currentSession != _navigationSessionId)
                    return false;

                if (!_navigationState.IsNavigating || _navigationState.HasArrived)
                    return false;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (_isUpdating)
                        return;

                    await UpdateNavigationAsync();
                });

                return true;
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task UpdateNavigationAsync()
    {
        if (_isUpdating)
            return;

        _isUpdating = true;

        try
        {
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));

            if (location == null)
            {
                StatusLabel.Text = "Cannot get current location.";
                return;
            }

            var userLocation = new Location(location.Latitude, location.Longitude);
            var destination = new Location(_destLat, _destLng);

            UpdatePins(userLocation, destination);

            double distanceKm = CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                destination.Latitude, destination.Longitude);

            double speedKph = 0;
            if (location.Speed.HasValue && location.Speed.Value > 0)
                speedKph = location.Speed.Value * 3.6;

            if (speedKph < 3)
                speedKph = 20;

            double etaMinutes = (distanceKm / speedKph) * 60.0;

            _navigationState.RemainingDistanceKm = distanceKm;
            _navigationState.CurrentSpeedKph = speedKph;
            _navigationState.EtaMinutes = etaMinutes;

            DistanceLabel.Text = $"Distance: {distanceKm:F2} km";
            SpeedLabel.Text = $"Speed: {speedKph:F1} km/h";
            EtaLabel.Text = $"ETA: {Math.Ceiling(etaMinutes)} min";
            StatusLabel.Text = "Navigating...";

            await DrawRouteOsrmAsync(userLocation, destination);

            if (distanceKm <= 0.01)
            {
                await CompleteArrivalAsync();
            }
            else
            {
                ArrivedPanel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Navigation update error: {ex.Message}");
            StatusLabel.Text = "Navigation update failed.";
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task CompleteArrivalAsync()
    {
        _navigationState.HasArrived = true;
        ArrivedPanel.IsVisible = true;
        StatusLabel.Text = "You arrived.";

        try
        {
            if (_reservationId > 0)
            {
                await _apiService.MarkArrivedAsync(_reservationId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MarkArrivedAsync failed: {ex.Message}");
        }

        if (!_hasReturnedAfterArrival)
        {
            _hasReturnedAfterArrival = true;
            await Task.Delay(1200);
            await Shell.Current.GoToAsync("..");
        }
    }

    private void UpdatePins(Location userLocation, Location destination)
    {
        map.Pins.Clear();

        _userPin = new Pin
        {
            Label = "You",
            Location = userLocation
        };

        _destinationPin = new Pin
        {
            Label = string.IsNullOrWhiteSpace(DestinationName)
                ? "Destination"
                : Uri.UnescapeDataString(DestinationName),
            Location = destination
        };

        map.Pins.Add(_userPin);
        map.Pins.Add(_destinationPin);
    }

    private async Task DrawRouteOsrmAsync(Location origin, Location destination)
    {
        try
        {
            string startLng = origin.Longitude.ToString(CultureInfo.InvariantCulture);
            string startLat = origin.Latitude.ToString(CultureInfo.InvariantCulture);
            string endLng = destination.Longitude.ToString(CultureInfo.InvariantCulture);
            string endLat = destination.Latitude.ToString(CultureInfo.InvariantCulture);

            string url =
                $"https://router.project-osrm.org/route/v1/driving/" +
                $"{startLng},{startLat};{endLng},{endLat}" +
                $"?overview=full&geometries=geojson";

            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);

            using var json = JsonDocument.Parse(response);

            var routes = json.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
            {
                StatusLabel.Text = "No route found.";
                return;
            }

            var coordinates = routes[0]
                .GetProperty("geometry")
                .GetProperty("coordinates");

            if (_routeLine != null)
                map.MapElements.Remove(_routeLine);

            _routeLine = new Polyline
            {
                StrokeColor = Colors.Blue,
                StrokeWidth = 6
            };

            double minLat = double.MaxValue;
            double maxLat = double.MinValue;
            double minLng = double.MaxValue;
            double maxLng = double.MinValue;

            foreach (var point in coordinates.EnumerateArray())
            {
                var lng = point[0].GetDouble();
                var lat = point[1].GetDouble();

                _routeLine.Geopath.Add(new Location(lat, lng));

                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lng < minLng) minLng = lng;
                if (lng > maxLng) maxLng = lng;
            }

            map.MapElements.Add(_routeLine);

            var center = new Location((minLat + maxLat) / 2, (minLng + maxLng) / 2);
            var latSpan = Math.Max(0.01, maxLat - minLat);
            var lngSpan = Math.Max(0.01, maxLng - minLng);

            map.MoveToRegion(new MapSpan(center, latSpan * 1.2, lngSpan * 1.2));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OSRM route error: {ex.Message}");
            StatusLabel.Text = "Failed to load route.";
        }
    }

    private void ClearMap()
    {
        if (_routeLine != null)
        {
            map.MapElements.Remove(_routeLine);
            _routeLine = null;
        }

        map.Pins.Clear();
        _userPin = null;
        _destinationPin = null;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) *
            Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private async void SimulateArrival_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Simulate Arrival",
            "Mark this navigation as arrived for testing?",
            "Yes",
            "No");

        if (!confirm)
            return;

        await CompleteArrivalAsync();
    }

    private async void CancelNavigation_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Cancel Navigation",
            "Are you sure you want to cancel?",
            "Yes",
            "No");

        if (!confirm)
            return;

        _navigationState.StopNavigation();
        _navigationSessionId++;
        _started = false;

        ClearMap();

        ArrivedPanel.IsVisible = false;
        DestinationLabel.Text = "Destination";
        DistanceLabel.Text = "Distance: --";
        SpeedLabel.Text = "Speed: --";
        EtaLabel.Text = "ETA: --";
        StatusLabel.Text = "Navigation cancelled.";

        await Shell.Current.GoToAsync("..");
    }
}