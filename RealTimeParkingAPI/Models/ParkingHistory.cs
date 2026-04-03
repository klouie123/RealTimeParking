namespace RealTimeParkingAPI.Models
{
    public class ParkingHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int ParkingSlotId { get; set; }
        public int ParkingLocationId { get; set; }

        public string ReservationReference { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }

        public DateTime ReservedAt { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public decimal PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        public string Status { get; set; } = string.Empty;

        public User? User { get; set; }
        public ParkingSlot? ParkingSlot { get; set; }
        public ParkingLocation? ParkingLocation { get; set; }
    }
}