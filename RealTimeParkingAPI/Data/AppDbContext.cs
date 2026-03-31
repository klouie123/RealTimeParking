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
        public DbSet<ParkingSlot> ParkingSlots { get; set; }
        public DbSet<ParkingReservation> ParkingReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ParkingSlot>()
                .HasOne(p => p.ParkingLocation)
                .WithMany()
                .HasForeignKey(p => p.ParkingLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ParkingReservation>()
                .HasOne(r => r.ParkingSlot)
                .WithMany()
                .HasForeignKey(r => r.ParkingSlotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ParkingReservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
