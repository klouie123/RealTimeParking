using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using RealTimeParkingApp.Models;
using RealTimeParkingApp.Services;

namespace RealTimeParkingApp.ViewModels;

public class MapViewModel : BaseViewModel
{
    private readonly ParkingService _parkingService;
    private readonly LocationService _locationService;

    public ParkingLocation? SelectedParking { get; set; }
    public ObservableCollection<ParkingLocation> Parkings { get; set; } = new();

    public ICommand FindParkingCommand { get; }

    // Max distance in km
    private const double MaxDistanceKm = 5;

    public MapViewModel(ParkingService parkingService, LocationService locationService)
    {
        _parkingService = parkingService;
        _locationService = locationService;
        FindParkingCommand = new Command(async () => await LoadNearestParkingAsync());
    }

    /// <summary>
    /// Load nearest parking using device location
    /// </summary>
    public async Task LoadNearestParkingAsync()
    {
        var location = await _locationService.GetCurrentLocationAsync();
        if (location == null) return;

        await LoadNearestParkingAsync(location.Latitude, location.Longitude);
    }

    /// <summary>
    /// Load nearest parking by coordinates and filter by radius
    /// </summary>
    public async Task LoadNearestParkingAsync(double lat, double lng)
    {
        try
        {
            Parkings.Clear();

            var parkings = await _parkingService.GetNearestAsync(lat, lng);

            if (parkings == null || !parkings.Any())
            {
                Debug.WriteLine("⚠️ No nearby parking found");
                return;
            }

            foreach (var p in parkings)
            {
                Parkings.Add(p);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ MapViewModel error: {ex.Message}");
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // km

        var dLat = Math.PI / 180 * (lat2 - lat1);
        var dLon = Math.PI / 180 * (lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(Math.PI / 180 * lat1) *
            Math.Cos(Math.PI / 180 * lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}