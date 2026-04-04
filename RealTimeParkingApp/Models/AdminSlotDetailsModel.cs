namespace RealTimeParkingApp.Models
{
    public class AdminSlotDetailsModel
    {
        public int SlotId { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReservedUser { get; set; }
        public DateTime? ReservedAt { get; set; }
        public DateTime? CheckInAt { get; set; }
        public string? ReservationReference { get; set; }
        public string? PaymentReference { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
    }
}