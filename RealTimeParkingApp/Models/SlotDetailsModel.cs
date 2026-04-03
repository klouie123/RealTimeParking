using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class SlotDetailsModel
    {
        public int Id { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? ReservedUser { get; set; }
        public DateTime? ReservedAt { get; set; }
        public DateTime? CheckInAt { get; set; }
        public string? ReservationReference { get; set; }
        public string? PaymentReference { get; set; }
    }
}
