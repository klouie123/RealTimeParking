using Microsoft.Maui.Devices.Sensors;

namespace RealTimeParkingApp.Services
{
    public class LocationService
    {
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync(
                        new GeolocationRequest(
                            GeolocationAccuracy.High,
                            TimeSpan.FromSeconds(10)));
                }

                return location;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex}");
                return null;
            }
        }
    }
}