namespace RealTimeParkingApp.Models
{
    public class LocationMaintenanceModel
    {
        public decimal ParkingPrice { get; set; }
        public List<AdminMaintenanceSlotItem> Slots { get; set; } = new();
    }

    public class AdminMaintenanceSlotItem
    {
        public int Id { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string EditableSlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public string StatusText =>
            IsActive
                ? $"Status: {Status} • Active"
                : $"Status: {Status} • Inactive";

        public string StatusBadgeColor =>
            Status.Equals("Available", StringComparison.OrdinalIgnoreCase) ? "#16A34A" :
            Status.Equals("Reserved", StringComparison.OrdinalIgnoreCase) ? "#D97706" :
            Status.Equals("Occupied", StringComparison.OrdinalIgnoreCase) ? "#DC2626" :
            "#64748B";

        public string ToggleButtonText => IsActive ? "Deactivate" : "Activate";
        public string ToggleButtonColor => IsActive ? "#B91C1C" : "#2563EB";
    }
}