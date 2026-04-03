namespace RealTimeParkingApp.Services
{
    public class NavigationStateService
    {
        public bool IsNavigating { get; private set; }
        public bool HasArrived { get; set; }

        public string DestinationName { get; private set; } = string.Empty;
        public double DestinationLat { get; private set; }
        public double DestinationLng { get; private set; }

        public double RemainingDistanceKm { get; set; }
        public double CurrentSpeedKph { get; set; }
        public double EtaMinutes { get; set; }

        public bool HasDestination =>
            !string.IsNullOrWhiteSpace(DestinationName) &&
            DestinationLat != 0 &&
            DestinationLng != 0;

        public void SetDestination(string destinationName, double lat, double lng)
        {
            DestinationName = destinationName ?? string.Empty;
            DestinationLat = lat;
            DestinationLng = lng;
        }

        public void StartNavigation(string destinationName, double lat, double lng)
        {
            SetDestination(destinationName, lat, lng);
            IsNavigating = true;
            HasArrived = false;
        }

        public void CancelNavigationRouteOnly()
        {
            IsNavigating = false;
            HasArrived = false;
            RemainingDistanceKm = 0;
            CurrentSpeedKph = 0;
            EtaMinutes = 0;
        }

        public void ClearAll()
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

        public void StopNavigation()
        {
            ClearAll();
        }

        public void Clear()
        {
            ClearAll();
        }
    }
}