namespace RealTimeParkingAPI.Models
{
    public class ParkingSlot
    {
        public int Id { get; set; }
        public int ParkingLocationId { get; set; }
        public string SlotCode { get; set; } = "";
        public string Status { get; set; } = "Available";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ParkingLocation? ParkingLocation { get; set; }
    }
}
