using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class ParkingSlot
    {
        public int Id { get; set; }
        public int ParkingLocationId { get; set; }
        public string SlotCode { get; set; } = "";
        public string Status { get; set; } = ""; // Available, Reserved, Occupied
        public bool IsActive { get; set; }
    }
}
