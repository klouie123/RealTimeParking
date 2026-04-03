namespace RealTimeParkingAPI.Models
{
    public class ParkingReservation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParkingSlotId { get; set; }

        public string ReservationReference { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }

        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public decimal PaymentAmount { get; set; } = 20m;
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        public string Status { get; set; } = "Reserved";
        // Reserved, Occupied, Completed, Cancelled, Expired

        public User? User { get; set; }
        public ParkingSlot? ParkingSlot { get; set; }
    }
}
