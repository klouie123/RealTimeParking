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
    public ObservableCollection<ParkingLocation> Parkings { get; } = new();

    public ICommand FindParkingCommand { get; }

    public MapViewModel(ParkingService parkingService, LocationService locationService)
    {
        _parkingService = parkingService;
        _locationService = locationService;
        FindParkingCommand = new Command(async () => await LoadNearestParkingAsync());
    }

    public async Task LoadNearestParkingAsync()
    {
        try
        {
            var location = await _locationService.GetCurrentLocationAsync();
            if (location == null)
                return;

            await LoadNearestParkingAsync(location.Latitude, location.Longitude);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadNearestParkingAsync error: {ex.Message}");
        }
    }

    public async Task LoadNearestParkingAsync(double lat, double lng)
    {
        try
        {
            Parkings.Clear();

            var parkings = await _parkingService.GetNearestAsync(lat, lng);

            if (parkings == null || !parkings.Any())
            {
                Debug.WriteLine("No nearby parking found.");
                return;
            }

            foreach (var p in parkings)
            {
                if (p == null)
                    continue;

                Parkings.Add(p);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MapViewModel error: {ex.Message}");
        }
    }
}