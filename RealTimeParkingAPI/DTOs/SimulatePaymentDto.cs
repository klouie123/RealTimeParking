namespace RealTimeParkingAPI.DTOs
{
    public class SimulatePaymentDto
    {
        public int ReservationId { get; set; }
        public string PaymentMethod { get; set; } = "Gcash";
    }
}
