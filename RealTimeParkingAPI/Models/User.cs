namespace RealTimeParkingAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Role { get; set; } = "User";

        // NULL for SuperAdmin or normal User
        public int? ParkingLocationId { get; set; }
        public ParkingLocation? ParkingLocation { get; set; }
    }
}
