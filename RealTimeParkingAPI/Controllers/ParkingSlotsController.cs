using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.DTO;
using RealTimeParkingAPI.Models;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingSlotsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParkingSlotsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("location/{parkingLocationId}")]
        public async Task<IActionResult> GetByLocation(int parkingLocationId)
        {
            var slots = await _context.ParkingSlots
                .Where(s => s.ParkingLocationId == parkingLocationId && s.IsActive)
                .OrderBy(s => s.SlotCode)
                .ToListAsync();

            return Ok(slots);
        }

        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReserveSlotRequest request)
        {
            var slot = await _context.ParkingSlots
                .FirstOrDefaultAsync(s => s.Id == request.ParkingSlotId);

            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            if (slot.Status != "Available")
                return BadRequest(new { message = "Slot is not available." });

            var existingReservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r =>
                    r.ParkingSlotId == request.ParkingSlotId &&
                    r.Status == "Active");

            if (existingReservation != null)
                return BadRequest(new { message = "Slot already reserved." });

            var reservation = new ParkingReservation
            {
                UserId = request.UserId,
                ParkingSlotId = request.ParkingSlotId,
                ReservedAt = DateTime.Now,
                Status = "Active"
            };

            slot.Status = "Reserved";

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Slot reserved successfully." });
        }

        [HttpPost("cancel/{parkingSlotId}")]
        public async Task<IActionResult> Cancel(int parkingSlotId)
        {
            var reservation = await _context.ParkingReservations
                .Where(r => r.ParkingSlotId == parkingSlotId && r.Status == "Active")
                .OrderByDescending(r => r.ReservedAt)
                .FirstOrDefaultAsync();

            var slot = await _context.ParkingSlots.FirstOrDefaultAsync(s => s.Id == parkingSlotId);

            if (reservation == null || slot == null)
                return NotFound(new { message = "Active reservation not found." });

            reservation.Status = "Cancelled";
            slot.Status = "Available";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Reservation cancelled." });
        }
    }
}
