using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.ViewModels;

namespace RealTimeParkingApp.Views;

[QueryProperty(nameof(Lat), "lat")]
[QueryProperty(nameof(Lng), "lng")]
public partial class MapPage : ContentPage
{
    private readonly MapViewModel vm;
    private Polyline? routeLine;
    private bool isMapReady = false;

    private double userLat;
    private double userLng;

    private readonly string googleMapsApiKey;

    public string Lat { get; set; }
    public string Lng { get; set; }

    private bool _initialized;

    public MapPage(MapViewModel viewModel, IConfiguration configuration)
    {
        InitializeComponent();
        vm = viewModel;
        BindingContext = vm;

        googleMapsApiKey = configuration["GoogleMapsApiKey"] ?? "";

        map.HandlerChanged += Map_HandlerChanged;
        vm.Parkings.CollectionChanged += Parkings_CollectionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
            return;

        if (!string.IsNullOrWhiteSpace(Lat) &&
            !string.IsNullOrWhiteSpace(Lng) &&
            double.TryParse(Lat, out var lat) &&
            double.TryParse(Lng, out var lng))
        {
            _initialized = true;
            await InitializeAsync(lat, lng);
        }
    }

    /// <summary>
    /// Set user's location and reload nearby parking
    /// </summary>
    public async Task SetUserLocationAsync(double lat, double lng)
    {
        userLat = lat;
        userLng = lng;

        if (map.Handler != null)
        {
            map.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(userLat, userLng),
                Distance.FromKilometers(1)));
        }

        await vm.LoadNearestParkingAsync(userLat, userLng);
        LoadMapPins();
    }

    public async Task InitializeAsync(double lat, double lng)
    {
        userLat = lat;
        userLng = lng;

        await WaitForMapReady();

        map.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(userLat, userLng),
            Distance.FromKilometers(1)));

        await vm.LoadNearestParkingAsync(userLat, userLng);
        LoadMapPins();
    }

    private void Map_HandlerChanged(object sender, EventArgs e)
    {
        if (map.Handler != null && !isMapReady)
        {
            isMapReady = true;
            LoadMapPins();
        }
    }

    private void Parkings_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        LoadMapPins();
    }

    private void LoadMapPins()
    {
        if (!isMapReady || vm.Parkings.Count == 0) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            map.Pins.Clear();

            foreach (var p in vm.Parkings)
            {
                if (p == null || double.IsNaN(p.Latitude) || double.IsNaN(p.Longitude))
                    continue;

                var pin = new Pin
                {
                    Label = $"{p.Name} ({p.Distance:F2} km)",
                    Location = new Location(p.Latitude, p.Longitude)
                };
                pin.MarkerClicked += (s, args) => SelectParkingAsync(p);
                map.Pins.Add(pin);
            }
        });
    }

    private async void SelectParkingAsync(ParkingLocation p)
    {
        vm.SelectedParking = p;
        var destination = new Location(p.Latitude, p.Longitude);

        map.MoveToRegion(MapSpan.FromCenterAndRadius(destination, Distance.FromKilometers(1)));
        await DrawRouteGoogleAsync(destination);
    }

    private async Task DrawRouteGoogleAsync(Location destination)
    {
        try
        {
            Debug.WriteLine(" DRAW ROUTE FUNCTION CALLED");

            if (userLat == 0 || userLng == 0)
            {
                await DisplayAlert("Error", "User location not set", "OK");
                return;
            }

            if (string.IsNullOrEmpty(googleMapsApiKey))
            {
                await DisplayAlert("DEBUG", $"KEY LENGTH: {googleMapsApiKey.Length}", "OK");
                await DisplayAlert("Error", "API Key missing", "OK");
                return;
            }

            var origin = $"{userLat},{userLng}";
            var dest = $"{destination.Latitude},{destination.Longitude}";
            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={dest}&key={googleMapsApiKey}&mode=driving";

            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);

            Debug.WriteLine(response);

            await DisplayAlert("Google API", response.Substring(0, 200), "OK");

            using var json = JsonDocument.Parse(response);
            var routes = json.RootElement.GetProperty("routes");

            if (routes.GetArrayLength() == 0)
            {
                await DisplayAlert("No Route", "No route found", "OK");
                return;
            }

            var points = routes[0]
                .GetProperty("overview_polyline")
                .GetProperty("points")
                .GetString();

            if (string.IsNullOrEmpty(points)) return;

            var locations = DecodePolyline(points);

            if (routeLine != null)
                map.MapElements.Remove(routeLine);

            routeLine = new Polyline
            {
                StrokeColor = Colors.Blue,
                StrokeWidth = 5
            };

            foreach (var loc in locations)
                routeLine.Geopath.Add(loc);

            map.MapElements.Add(routeLine);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Google Route error: {ex.Message}");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    //helpers
    private async Task WaitForMapReady()
    {
        int tries = 0;

        while (map.Handler == null && tries < 10)
        {
            await Task.Delay(100);
            tries++;
        }
    }

    private List<Location> DecodePolyline(string polyline)
    {
        var poly = new List<Location>();
        int index = 0, lat = 0, lng = 0;

        while (index < polyline.Length)
        {
            int result = 1, shift = 0, b;
            do
            {
                b = polyline[index++] - 63 - 1;
                result += b << shift;
                shift += 5;
            } while (b >= 0x1f);
            lat += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

            result = 1;
            shift = 0;
            do
            {
                b = polyline[index++] - 63 - 1;
                result += b << shift;
                shift += 5;
            } while (b >= 0x1f);
            lng += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

            poly.Add(new Location(lat * 1e-5, lng * 1e-5));
        }

        return poly;
    }

    
    //helpers

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ParkingLocation p)
        {
            vm.SelectedParking = p;

            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(
                p.Latitude,
                p.Longitude,
                new MapLaunchOptions
                {
                    Name = p.Name,
                    NavigationMode = NavigationMode.Driving
                });
        }
    }

    private void OnParkingTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ParkingLocation p)
            SelectParkingAsync(p);
    }
}