namespace RealTimeParkingAPI.Models
{
    public class ParkingLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int AllowedMinutes { get; set; }
        public string Description { get; set; } = "";

        // payment config
        public string? PaymentProvider { get; set; } // e.g. "GCash"
        public string? MerchantDisplayName { get; set; }
        public string? MerchantGcashNumber { get; set; }

        public decimal ParkingPrice { get; set; } = 20m;

        // for future real hosted checkout
        public string? MerchantPaymentUrl { get; set; }

        // optional merchant QR image / QR payload later
        public string? MerchantQrText { get; set; }

        public ICollection<User> AdminUsers { get; set; } = new List<User>();
        public ICollection<ParkingSlot> ParkingSlots { get; set; } = new List<ParkingSlot>();
    }
}