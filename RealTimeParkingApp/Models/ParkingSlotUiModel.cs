using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class ParkingSlotUiModel
    {
        public int Id { get; set; }
        public string SlotCode { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsAvailable { get; set; }

        public string SlotBackgroundColor { get; set; } = "#FFFFFF";
        public string SlotBorderColor { get; set; } = "#E5E7EB";
        public string SlotTextColor { get; set; } = "#111827";
        public string SlotSubTextColor { get; set; } = "#6B7280";
        public string StatusBadgeColor { get; set; } = "#16A34A";
        public string SlotMessage { get; set; } = "";
        public double SlotOpacity { get; set; } = 1.0;
    }
}
