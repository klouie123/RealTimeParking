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
            var url = $"{ApiConfig.BaseUrl}ParkingLocations";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ParkingLocation>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ParkingLocation>>(json) ?? new List<ParkingLocation>();
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
            var url = $"{ApiConfig.BaseUrl}ParkingSlots/location/{parkingLocationId}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<ParkingSlot>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ParkingSlot>>(json) ?? new List<ParkingSlot>();
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
            var url = $"{ApiConfig.BaseUrl}ParkingSlots/reserve";

            var payload = new
            {
                UserId = userId,
                ParkingSlotId = parkingSlotId
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

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
            var url = $"{ApiConfig.BaseUrl}Reservations/user/{userId}/active";
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

    public async Task<ParkingLocation?> GetParkingLocationByIdAsync(int parkingLocationId)
    {
        try
        {
            var response = await _http.GetAsync($"{ApiConfig.BaseUrl}/parkinglocations/{parkingLocationId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ParkingLocation>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CancelReservationAsync(int reservationId)
    {
        try
        {
            var url = $"{ApiConfig.BaseUrl}Reservations/{reservationId}/cancel";
            var response = await _http.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
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