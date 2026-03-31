using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public int ParkingLocationId { get; set; }
        public string ParkingLocationName { get; set; }
        public string SlotCode { get; set; }
        public string Status { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
