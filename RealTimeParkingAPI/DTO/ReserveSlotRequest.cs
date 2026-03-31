namespace RealTimeParkingAPI.DTO
{
    public class ReserveSlotRequest
    {
        public int UserId { get; set; }
        public int ParkingSlotId { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
