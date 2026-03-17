using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Models;


namespace RealTimeParkingAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<ParkingLocation> ParkingLocations { get; set; }
        public DbSet<ParkingHistory> ParkingHistories { get; set; }
    }
}
