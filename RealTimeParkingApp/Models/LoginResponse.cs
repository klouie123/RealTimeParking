namespace RealTimeParkingApp.Models;

public class LoginResponse
{
    public bool IsSuccess { get; set; }
    public string Token { get; set; } = "";
    public string Role { get; set; } = "";
}