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

        public ICollection<User> AdminUsers { get; set; } = new List<User>();
        public ICollection<ParkingSlot> ParkingSlots { get; set; } = new List<ParkingSlot>();
    }
}