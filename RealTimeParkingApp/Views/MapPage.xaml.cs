using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;
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

    public string Lat { get; set; }
    public string Lng { get; set; }

    private bool _initialized;

    private readonly NavigationStateService _navigationState;

    public MapPage(MapViewModel viewModel, IConfiguration configuration, NavigationStateService navigationState)
    {
        InitializeComponent();

        vm = viewModel;
        BindingContext = vm;

        _navigationState = navigationState;

        map.HandlerChanged += Map_HandlerChanged;
        vm.Parkings.CollectionChanged += Parkings_CollectionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // RESET EVERY TIME
        ResetMap();

        if (!string.IsNullOrWhiteSpace(Lat) &&
            !string.IsNullOrWhiteSpace(Lng) &&
            double.TryParse(Lat, out var lat) &&
            double.TryParse(Lng, out var lng))
        {
            await InitializeAsync(lat, lng);
        }
    }

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
        if (!isMapReady)
            return;

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

                pin.MarkerClicked += (s, args) =>
                {
                    SelectParkingAsync(p);
                };

                map.Pins.Add(pin);
            }

            // optional: show user pin
            //if (userLat != 0 && userLng != 0)
            //{
            //    map.Pins.Add(new Pin
            //    {
            //        Label = "You are here",
            //        Location = new Location(userLat, userLng)
            //    });
            //}
        });
    }

    private async void SelectParkingAsync(ParkingLocation p)
    {
        vm.SelectedParking = p;

        var destination = new Location(p.Latitude, p.Longitude);

        map.MoveToRegion(MapSpan.FromCenterAndRadius(
            destination,
            Distance.FromKilometers(1)));

        await DrawRouteOsrmAsync(destination);
    }

    private async Task DrawRouteOsrmAsync(Location destination)
    {
        try
        {
            Debug.WriteLine("OSRM ROUTE FUNCTION CALLED");

            if (userLat == 0 || userLng == 0)
            {
                await DisplayAlert("Error", "User location not set", "OK");
                return;
            }

            string startLng = userLng.ToString(CultureInfo.InvariantCulture);
            string startLat = userLat.ToString(CultureInfo.InvariantCulture);
            string endLng = destination.Longitude.ToString(CultureInfo.InvariantCulture);
            string endLat = destination.Latitude.ToString(CultureInfo.InvariantCulture);

            string url =
                $"https://router.project-osrm.org/route/v1/driving/" +
                $"{startLng},{startLat};{endLng},{endLat}" +
                $"?overview=full&geometries=geojson";

            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);

            Debug.WriteLine("==== OSRM RESPONSE ====");
            Debug.WriteLine(response);
            Debug.WriteLine("==== END OSRM RESPONSE ====");

            using var json = JsonDocument.Parse(response);

            var routes = json.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
            {
                await DisplayAlert("No Route", "No route found", "OK");
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
                StrokeWidth = 5
            };

            double minLat = double.MaxValue;
            double maxLat = double.MinValue;
            double minLng = double.MaxValue;
            double maxLng = double.MinValue;

            foreach (var point in coordinates.EnumerateArray())
            {
                double lng = point[0].GetDouble();
                double lat = point[1].GetDouble();

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
            Debug.WriteLine($"OSRM Route error: {ex.Message}");
            await DisplayAlert("Route Error", ex.Message, "OK");
        }
    }

    private async Task WaitForMapReady()
    {
        int tries = 0;

        while (map.Handler == null && tries < 10)
        {
            await Task.Delay(100);
            tries++;
        }
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ParkingLocation p)
        {
            vm.SelectedParking = p;

            _navigationState.StartNavigation(p.Name ?? "Destination", p.Latitude, p.Longitude);

            string name = Uri.EscapeDataString(p.Name ?? "Destination");

            await Shell.Current.GoToAsync(
                $"{nameof(NavigationMapPage)}" +
                $"?destLat={p.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&destLng={p.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&destName={name}");
        }
    }

    private void OnParkingTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ParkingLocation p)
            SelectParkingAsync(p);
    }

    private void ResetMap()
    {
        // Clear route
        if (routeLine != null)
        {
            map.MapElements.Remove(routeLine);
            routeLine = null;
        }

        // Clear pins
        map.Pins.Clear();

        // Clear ViewModel data
        vm.Parkings.Clear();

        vm.SelectedParking = null;

        userLat = 0;
        userLng = 0;

        _initialized = false;
    }
}