using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.DTOs;
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
            //await ReleaseExpiredReservationsAsync();

            var slots = await _context.ParkingSlots
                .Where(s => s.ParkingLocationId == parkingLocationId && s.IsActive)
                .OrderBy(s => s.SlotCode)
                .ToListAsync();

            return Ok(slots);
        }

        //[HttpPost("reserve")]
        //public async Task<IActionResult> Reserve([FromBody] ReserveSlotRequest request)
        //{
        //    await ReleaseExpiredReservationsAsync();

        //    var slot = await _context.ParkingSlots
        //        .FirstOrDefaultAsync(s => s.Id == request.ParkingSlotId && s.IsActive);

        //    if (slot == null)
        //        return NotFound(new { message = "Slot not found." });

        //    if (slot.Status != "Available")
        //        return BadRequest(new { message = "Slot is not available." });

            // Check if this user already has an active reservation
        //    var userHasActiveReservation = await _context.ParkingReservations
        //        .AnyAsync(r => r.UserId == request.UserId && r.Status == "Active");

        //   if (userHasActiveReservation)
        //        return BadRequest(new { message = "User already has an active reservation." });

            // Check if slot already has active reservation
        //    var existingReservation = await _context.ParkingReservations
        //        .FirstOrDefaultAsync(r =>
        //            r.ParkingSlotId == request.ParkingSlotId &&
        //            r.Status == "Active");

        //    if (existingReservation != null)
        //        return BadRequest(new { message = "Slot already reserved." });

        //    var now = DateTime.UtcNow;

        //    var reservation = new ParkingReservation
        //    {
        //        UserId = request.UserId,
        //        ParkingSlotId = request.ParkingSlotId,
        //        ReservedAt = now,
        //        ExpiresAt = now.AddHours(1),
        //        Status = "Active"
        //    };

        //    slot.Status = "Reserved";

        //    _context.ParkingReservations.Add(reservation);
        //    await _context.SaveChangesAsync();

        //    return Ok(new
        //    {
        //        message = "Slot reserved successfully.",
        //        reservationId = reservation.Id,
        //        reservedAt = reservation.ReservedAt,
        //        expiresAt = reservation.ExpiresAt
        //    });
        //}

        //[HttpPost("cancel/{parkingSlotId}")]
        //public async Task<IActionResult> Cancel(int parkingSlotId)
        //{
        //    await ReleaseExpiredReservationsAsync();

        //    var reservation = await _context.ParkingReservations
        //        .Where(r => r.ParkingSlotId == parkingSlotId && r.Status == "Active")
        //        .OrderByDescending(r => r.ReservedAt)
        //        .FirstOrDefaultAsync();

        //    var slot = await _context.ParkingSlots
        //        .FirstOrDefaultAsync(s => s.Id == parkingSlotId);

        //    if (reservation == null || slot == null)
        //        return NotFound(new { message = "Active reservation not found." });

        //    reservation.Status = "Cancelled";
        //    slot.Status = "Available";

        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Reservation cancelled." });
        //}

        //private async Task ReleaseExpiredReservationsAsync()
        //{
        //    try
        //    {
        //        Console.WriteLine("ReleaseExpiredReservationsAsync START");

        //        var now = DateTime.UtcNow;

        //        var expiredReservations = await _context.ParkingReservations
        //            .Include(r => r.ParkingSlot)
        //            .Where(r => r.Status == "Reserved" && r.ExpiresAt <= now)
        //            .ToListAsync();

        //        Console.WriteLine($"Expired reservations found: {expiredReservations.Count}");

        //        if (!expiredReservations.Any())
        //            return;

        //        foreach (var reservation in expiredReservations)
        //        {
        //            reservation.Status = "Expired";

        //            if (reservation.ParkingSlot != null)
        //            {
        //                reservation.ParkingSlot.Status = "Available";
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("ReleaseExpiredReservationsAsync ERROR:");
        //        Console.WriteLine(ex.ToString());
        //    }
        //}
    }
}