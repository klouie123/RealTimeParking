namespace RealTimeParkingApp.Models;

public class LoginResponse
{
    public bool IsSuccess { get; set; }
    public int UserId { get; set; }
    public string? Token { get; set; }
    public string? Role { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public int? ParkingLocationId { get; set; }
    public string? ParkingLocationName { get; set; }
}