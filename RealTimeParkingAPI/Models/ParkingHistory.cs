namespace RealTimeParkingAPI.Models
{
    public class ParkingHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParkingLocationId { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
        public string Status { get; set; } = "";
    }
}