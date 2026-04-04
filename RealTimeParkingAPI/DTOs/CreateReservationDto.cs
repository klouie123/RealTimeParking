namespace RealTimeParkingAPI.DTOs
{
    public class CreateReservationDto
    {
        public int ParkingSlotId { get; set; }
        public int ParkingLocationId { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }
}
