namespace RealTimeParkingAPI.DTOs
{
    public class LocationMaintenanceDto
    {
        public decimal ParkingPrice { get; set; }
        public List<LocationMaintenanceSlotDto> Slots { get; set; } = new();
    }

    public class LocationMaintenanceSlotDto
    {
        public int Id { get; set; }
        public string SlotCode { get; set; } = string.Empty;
        public string EditableSlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UpdateParkingPriceDto
    {
        public decimal ParkingPrice { get; set; }
    }

    public class RenameSlotDto
    {
        public string SlotCode { get; set; } = string.Empty;
    }

    public class AddLocationSlotDto
    {
        public string SlotCode { get; set; } = string.Empty;
    }

    public class ToggleSlotActiveDto
    {
        public bool IsActive { get; set; }
    }
}