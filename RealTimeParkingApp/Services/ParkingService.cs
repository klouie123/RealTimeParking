using System.Diagnostics;
using Newtonsoft.Json;
using RealTimeParkingApp.Config;
using RealTimeParkingApp.Models;

namespace RealTimeParkingApp.Services;

public class ParkingService
{
    private readonly HttpClient _http;

    public ParkingService()
    {
        _http = new HttpClient();
    }

    public async Task<List<ParkingLocation>> GetNearestAsync(double lat, double lng)
    {
        try
        {
            var url = $"{ApiConfig.BaseUrl}parking/nearest?lat={lat}&lng={lng}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ParkingLocation>();

            var json = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<ParkingLocation>>(json)
                   ?? new List<ParkingLocation>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API ERROR: {ex.Message}");
            return new List<ParkingLocation>();
        }
    }
}