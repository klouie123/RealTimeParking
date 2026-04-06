namespace RealTimeParkingAPI.Services
{
    public class EmailSettings
    {
        public string FromName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
    }
}