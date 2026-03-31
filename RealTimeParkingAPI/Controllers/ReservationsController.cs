using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("user/{userId}/active")]
        public async Task<IActionResult> GetActiveReservation(int userId)
        {
            await ReleaseExpiredReservationsAsync();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingLocation)
                .Where(r => r.UserId == userId && r.Status == "Active")
                .OrderByDescending(r => r.ReservedAt)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.ParkingSlotId,
                    r.ReservedAt,
                    r.ExpiresAt,
                    r.Status,
                    SlotCode = r.ParkingSlot!.SlotCode,
                    ParkingLocationId = r.ParkingSlot.ParkingLocationId,
                    ParkingLocationName = r.ParkingSlot.ParkingLocation!.Name,
                    Latitude = r.ParkingSlot.ParkingLocation.Latitude,
                    Longitude = r.ParkingSlot.ParkingLocation.Longitude
                })
                .FirstOrDefaultAsync();

            if (reservation == null)
                return NotFound(new { message = "No active reservation." });

            return Ok(reservation);
        }

        [HttpPost("{reservationId}/cancel")]
        public async Task<IActionResult> CancelReservation(int reservationId)
        {
            await ReleaseExpiredReservationsAsync();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.Status == "Active");

            if (reservation == null)
                return NotFound(new { message = "Active reservation not found." });

            reservation.Status = "Cancelled";

            if (reservation.ParkingSlot != null)
            {
                reservation.ParkingSlot.Status = "Available";
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Reservation cancelled successfully." });
        }

        private async Task ReleaseExpiredReservationsAsync()
        {
            var now = DateTime.UtcNow;

            var expiredReservations = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .Where(r => r.Status == "Active" && r.ExpiresAt <= now)
                .ToListAsync();

            if (!expiredReservations.Any())
                return;

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = "Expired";

                if (reservation.ParkingSlot != null)
                {
                    reservation.ParkingSlot.Status = "Available";
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}