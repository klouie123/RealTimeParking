using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class LocationAdminSlotDisplayModel
    {
        public int Id { get; set; }
        public int ParkingLocationId { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Color StatusBadgeColor { get; set; } = Colors.Gray;
        public string SlotMessage { get; set; } = string.Empty;
    }
}
