using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class ParkingHistoryItem
    {
        public int Id { get; set; }
        public string ParkingLocationName { get; set; } = string.Empty;
        public string SlotCode { get; set; } = string.Empty;
        public string ReservationReference { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime? CheckInAt { get; set; }
        public DateTime? CheckOutAt { get; set; }
        public decimal PaymentAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
