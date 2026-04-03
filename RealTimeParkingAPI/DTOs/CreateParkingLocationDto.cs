namespace RealTimeParkingAPI.DTOs
{
    public class CreateParkingLocationDto
    {
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int AllowedMinutes { get; set; }
        public string Description { get; set; } = "";
    }
}
