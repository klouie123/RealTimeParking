namespace RealTimeParkingAPI.Models
{
    public class ParkingReservation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParkingSlotId { get; set; }
        public DateTime? ReservedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string Status { get; set; } = "Active";

        public User? User { get; set; }
        public ParkingSlot? ParkingSlot { get; set; }
    }
}
