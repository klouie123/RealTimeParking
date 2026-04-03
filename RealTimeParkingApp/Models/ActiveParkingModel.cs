namespace RealTimeParkingApp.Models
{
    public class ActiveParkingModel
    {
        public int ReservationId { get; set; }
        public int ParkingSlotId { get; set; }
        public int ParkingLocationId { get; set; }

        public string ParkingLocationName { get; set; } = string.Empty;
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string ReservationReference { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }

        public decimal PaymentAmount { get; set; }

        public DateTime ReservedAt { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}