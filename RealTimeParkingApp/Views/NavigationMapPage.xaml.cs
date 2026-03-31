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
public partial class NavigationMapPage : ContentPage
{
    private readonly NavigationStateService _navigationState;

    private Polyline? routeLine;
    private Pin? userPin;
    private Pin? destinationPin;

    private bool _started;
    private int _navigationSessionId = 0;

    public string DestinationLat { get; set; }
    public string DestinationLng { get; set; }
    public string DestinationName { get; set; }

    private double _destLat;
    private double _destLng;

    public NavigationMapPage(NavigationStateService navigationState)
    {
        InitializeComponent();
        _navigationState = navigationState;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!double.TryParse(DestinationLat, NumberStyles.Any, CultureInfo.InvariantCulture, out _destLat) ||
            !double.TryParse(DestinationLng, NumberStyles.Any, CultureInfo.InvariantCulture, out _destLng))
        {
            await DisplayAlert("Error", "Invalid destination coordinates", "OK");
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
                await DisplayAlert("Permission", "Location permission denied", "OK");
                return;
            }

            _navigationSessionId++;
            int currentSession = _navigationSessionId;

            // full UI reset
            ArrivedPanel.IsVisible = false;
            StatusLabel.Text = "Loading route...";
            DistanceLabel.Text = "Distance:";
            SpeedLabel.Text = "Speed:";
            EtaLabel.Text = "ETA:";

            if (routeLine != null)
            {
                map.MapElements.Remove(routeLine);
                routeLine = null;
            }

            map.Pins.Clear();
            userPin = null;
            destinationPin = null;

            await UpdateNavigationAsync();

            Device.StartTimer(TimeSpan.FromSeconds(4), () =>
            {
                if (currentSession != _navigationSessionId)
                    return false;

                if (!_navigationState.IsNavigating || _navigationState.HasArrived)
                    return false;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (currentSession != _navigationSessionId)
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
            {
                speedKph = location.Speed.Value * 3.6;
            }

            if (speedKph < 3)
            {
                speedKph = 20;
            }

            double etaMinutes = (distanceKm / speedKph) * 60.0;

            _navigationState.RemainingDistanceKm = distanceKm;
            _navigationState.CurrentSpeedKph = speedKph;
            _navigationState.EtaMinutes = etaMinutes;

            DistanceLabel.Text = $"Distance: {distanceKm:F2} km";
            SpeedLabel.Text = $"Speed: {speedKph:F1} km/h";
            EtaLabel.Text = $"ETA: {Math.Ceiling(etaMinutes)} min";
            StatusLabel.Text = "Navigating.";

            await DrawRouteOsrmAsync(userLocation, destination);

            if (distanceKm <= 0.01)
            {
                _navigationState.HasArrived = true;
                ArrivedPanel.IsVisible = true;
                StatusLabel.Text = "You arrived";
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
    }

    private void UpdatePins(Location userLocation, Location destination)
    {
        map.Pins.Clear();

        userPin = new Pin
        {
            Label = "You",
            Location = userLocation
        };

        destinationPin = new Pin
        {
            Label = string.IsNullOrWhiteSpace(DestinationName)
                ? "Destination"
                : Uri.UnescapeDataString(DestinationName),
            Location = destination
        };

        map.Pins.Add(userPin);
        map.Pins.Add(destinationPin);
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

            if (routeLine != null)
                map.MapElements.Remove(routeLine);

            routeLine = new Polyline
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

                routeLine.Geopath.Add(new Location(lat, lng));

                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lng < minLng) minLng = lng;
                if (lng > maxLng) maxLng = lng;
            }

            map.MapElements.Add(routeLine);

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

    private async void BackToDashboard_Clicked(object sender, EventArgs e)
    {
        _navigationState.StopNavigation();
        _navigationSessionId++;
        await Shell.Current.GoToAsync(".");
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

        if (routeLine != null)
        {
            map.MapElements.Remove(routeLine);
            routeLine = null;
        }

        map.Pins.Clear();
        userPin = null;
        destinationPin = null;

        ArrivedPanel.IsVisible = false;
        DestinationLabel.Text = "Destination";
        DistanceLabel.Text = "Distance:";
        SpeedLabel.Text = "Speed:";
        EtaLabel.Text = "ETA:";
        StatusLabel.Text = "Navigation cancelled.";

        await Shell.Current.GoToAsync("..");
    }
}