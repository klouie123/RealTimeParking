using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Devices.Sensors;

namespace RealTimeParkingApp.Services
{
    public class LocationService
    {
        public async Task<Location> GetCurrentLocationAsync()
        {
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location == null)
            {
                location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.High));
            }

            return location;
        }
    }
}
