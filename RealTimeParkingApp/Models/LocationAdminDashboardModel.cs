using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class LocationAdminDashboardModel
    {
        public string ParkingLocationName { get; set; } = "";
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int ReservedSlots { get; set; }
        public int ActiveReservations { get; set; }
    }
}
