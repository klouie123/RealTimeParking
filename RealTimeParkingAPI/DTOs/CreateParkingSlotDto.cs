namespace RealTimeParkingAPI.DTOs
{
    public class CreateParkingSlotDto
    {
        public int ParkingLocationId { get; set; }
        public string SlotCode { get; set; } = "";
    }
}
