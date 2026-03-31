using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class ActiveReservation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParkingSlotId { get; set; }
        public DateTime ReservedAt { get; set; }
        public string Status { get; set; } = "";
        public string SlotCode { get; set; } = "";
        public int ParkingLocationId { get; set; }
        public string ParkingLocationName { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
