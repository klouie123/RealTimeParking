using System.Diagnostics;
using System.Text;
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

    public async Task<List<ParkingLocation>> GetParkingLocationsAsync()
    {
        try
        {
            var url = $"{ApiConfig.BaseUrl}parkinglocations";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ParkingLocation>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ParkingLocation>>(json)
                   ?? new List<ParkingLocation>();
        }
        catch
        {
            return new List<ParkingLocation>();
        }
    }

    public async Task<List<ParkingSlot>> GetSlotsByLocationAsync(int parkingLocationId)
    {
        try
        {
            var url = $"{ApiConfig.BaseUrl}parkingslots/location/{parkingLocationId}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ParkingSlot>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ParkingSlot>>(json)
                   ?? new List<ParkingSlot>();
        }
        catch
        {
            return new List<ParkingSlot>();
        }
    }

    public async Task<bool> ReserveSlotAsync(int userId, int parkingSlotId)
    {
        try
        {
            var url = $"{ApiConfig.BaseUrl}parkingslots/reserve";

            var body = new
            {
                UserId = userId,
                ParkingSlotId = parkingSlotId
            };

            var jsonBody = JsonConvert.SerializeObject(body);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ActiveReservation?> GetActiveReservationAsync(int userId)
    {
        try
        {
            var url = $"{ApiConfig.BaseUrl}reservations/user/{userId}/active";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ActiveReservation>(json);
        }
        catch
        {
            return null;
        }
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