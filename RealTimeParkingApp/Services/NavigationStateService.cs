using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Services
{
    public class NavigationStateService
    {
        public bool IsNavigating { get; set; }
        public bool HasArrived { get; set; }

        public string DestinationName { get; set; } = string.Empty;
        public double DestinationLat { get; set; }
        public double DestinationLng { get; set; }

        public double RemainingDistanceKm { get; set; }
        public double CurrentSpeedKph { get; set; }
        public double EtaMinutes { get; set; }

        public void StartNavigation(string destinationName, double lat, double lng)
        {
            IsNavigating = true;
            HasArrived = false;

            DestinationName = destinationName ?? string.Empty;
            DestinationLat = lat;
            DestinationLng = lng;

            RemainingDistanceKm = 0;
            CurrentSpeedKph = 0;
            EtaMinutes = 0;
        }

        public void StopNavigation()
        {
            IsNavigating = false;
            HasArrived = false;

            DestinationName = string.Empty;
            DestinationLat = 0;
            DestinationLng = 0;

            RemainingDistanceKm = 0;
            CurrentSpeedKph = 0;
            EtaMinutes = 0;
        }
    }
}
