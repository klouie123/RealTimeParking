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
    }
}
