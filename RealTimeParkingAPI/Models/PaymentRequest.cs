namespace RealTimeParkingAPI.Models
{
    public class PaymentRequest
    {
        public int Id { get; set; }

        public int ReservationId { get; set; }
        public int UserId { get; set; }
        public int ParkingLocationId { get; set; }

        public string Provider { get; set; } = "GCash";
        public string PaymentReference { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        // Pending, Paid, Failed, Expired

        public string? ExternalPaymentUrl { get; set; }
        public string? MerchantDisplayName { get; set; }
        public string? MerchantGcashNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public ParkingReservation? Reservation { get; set; }
        public User? User { get; set; }
        public ParkingLocation? ParkingLocation { get; set; }
    }
}