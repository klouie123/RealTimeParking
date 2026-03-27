using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class ParkingLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int AllowedMinutes { get; set; }
        public string? Description { get; set; }
        public double Distance { get; set; } // from API
    }
}
