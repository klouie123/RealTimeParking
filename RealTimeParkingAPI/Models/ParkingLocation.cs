namespace RealTimeParkingAPI.Models
{
    public class ParkingLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int AllowedMinutes { get; set; }
        public string Description { get; set; } = "";
    }
}