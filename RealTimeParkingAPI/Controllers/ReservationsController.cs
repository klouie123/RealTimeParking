using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.DTOs;
using RealTimeParkingAPI.Models;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateReservation(CreateReservationDto dto)
        {
            await ReleaseExpiredReservationsAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == dto.ParkingSlotId);

            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            if (!slot.IsActive)
                return BadRequest(new { message = "Slot is inactive." });

            if (slot.Status != "Available")
                return BadRequest(new { message = "Slot is not available." });

            var existingActiveReservation = await _context.ParkingReservations
               .AnyAsync(r => r.UserId == userId &&
                   (r.Status == "Reserved" || r.Status == "Occupied"));

            if (existingActiveReservation)
                return BadRequest(new { message = "User already has an active reservation." });

            var reservation = new ParkingReservation
            {
                UserId = userId,
                ParkingSlotId = dto.ParkingSlotId,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Status = "Reserved",
                ReservationReference = GenerateReservationReference(),
                PaymentAmount = 20m,
                PaymentStatus = "Pending"
            };

            slot.Status = "Reserved";

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Reservation created successfully.",
                reservationId = reservation.Id,
                //qrCodeValue = reservation.QrCodeValue,
                paymentMethod = reservation.PaymentMethod,
                paymentStatus = reservation.PaymentStatus,
                expiresAt = reservation.ExpiresAt
            });
        }

        [HttpGet("user/{userId}/active")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetActiveReservation(int userId)
        {
            await ReleaseExpiredReservationsAsync();

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
                return Unauthorized();

            if (currentUserId != userId)
                return Forbid();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingLocation)
                .Where(r => r.UserId == userId &&
                           (r.Status == "Reserved" || r.Status == "Occupied"))
                .OrderByDescending(r => r.ReservedAt)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.ParkingSlotId,
                    r.ReservedAt,
                    r.ExpiresAt,
                    r.CheckInAt,
                    r.CheckOutAt,
                    r.Status,
                    r.PaymentMethod,
                    r.PaymentStatus,
                    r.PaymentReference,
                    //r.QrCodeValue,
                    SlotCode = r.ParkingSlot != null ? r.ParkingSlot.SlotCode : "",
                    ParkingLocationId = r.ParkingSlot != null ? r.ParkingSlot.ParkingLocationId : 0,
                    ParkingLocationName = r.ParkingSlot != null && r.ParkingSlot.ParkingLocation != null
                        ? r.ParkingSlot.ParkingLocation.Name
                        : "",
                    Latitude = r.ParkingSlot != null && r.ParkingSlot.ParkingLocation != null
                        ? r.ParkingSlot.ParkingLocation.Latitude
                        : 0,
                    Longitude = r.ParkingSlot != null && r.ParkingSlot.ParkingLocation != null
                        ? r.ParkingSlot.ParkingLocation.Longitude
                        : 0
                })
                .FirstOrDefaultAsync();

            if (reservation == null)
                return NotFound(new { message = "No active reservation." });

            return Ok(reservation);
        }

        [HttpPost("{reservationId}/cancel")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CancelReservation(int reservationId)
        {
            await ReleaseExpiredReservationsAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r =>
                    r.Id == reservationId &&
                    r.UserId == userId &&
                    r.Status == "Reserved");

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

        [HttpPut("check-out")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> CheckOut(CheckOutDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int locationAdminId))
                return Unauthorized();

            var currentAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Id == locationAdminId);
            if (currentAdmin == null || currentAdmin.ParkingLocationId == null)
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r => r.Id == dto.ReservationId);

            if (reservation == null)
                return NotFound(new { message = "Reservation not found." });

            if (reservation.ParkingSlot == null)
                return BadRequest(new { message = "Reservation slot is missing." });

            if (reservation.ParkingSlot.ParkingLocationId != currentAdmin.ParkingLocationId.Value)
                return Forbid();

            if (reservation.Status != "Occupied")
                return BadRequest(new { message = "Only occupied reservations can be checked out." });

            reservation.Status = "Completed";
            reservation.CheckOutAt = DateTime.UtcNow;
            reservation.ParkingSlot.Status = "Available";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Check-out successful.",
                reservationId = reservation.Id,
                status = reservation.Status
            });
        }

        [HttpGet("my-location-reservations")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> GetMyLocationReservations()
        {
            await ReleaseExpiredReservationsAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int locationAdminId))
                return Unauthorized();

            var currentAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Id == locationAdminId);
            if (currentAdmin == null || currentAdmin.ParkingLocationId == null)
                return Unauthorized();

            var reservations = await _context.ParkingReservations
                .Include(r => r.User)
                .Include(r => r.ParkingSlot)
                .Where(r => r.ParkingSlot != null &&
                            r.ParkingSlot.ParkingLocationId == currentAdmin.ParkingLocationId.Value &&
                            (r.Status == "Reserved" || r.Status == "Occupied"))
                .Select(r => new
                {
                    r.Id,
                    Username = r.User != null ? r.User.Username : "",
                    SlotCode = r.ParkingSlot != null ? r.ParkingSlot.SlotCode : "",
                    r.Status,
                    r.PaymentMethod,
                    r.PaymentStatus,
                    r.PaymentReference,
                    r.ReservedAt,
                    r.ExpiresAt,
                    r.CheckInAt,
                    //r.QrCodeValue
                })
                .OrderByDescending(r => r.ReservedAt)
                .ToListAsync();

            return Ok(reservations);
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

        [HttpGet("my-active")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyActiveParking()
        {
            await ReleaseExpiredReservationsAsync();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                    .ThenInclude(s => s.ParkingLocation)
                .Where(r => r.UserId == userId &&
                            (r.Status == "Reserved" || r.Status == "Occupied"))
                .OrderByDescending(r => r.ReservedAt)
                .Select(r => new ActiveParkingDto
                {
                    ReservationId = r.Id,
                    ParkingSlotId = r.ParkingSlotId,
                    ParkingLocationId = r.ParkingSlot != null ? r.ParkingSlot.ParkingLocationId : 0,
                    ParkingLocationName = r.ParkingSlot != null && r.ParkingSlot.ParkingLocation != null
                                        ? r.ParkingSlot.ParkingLocation.Name
                                        : "",
                    SlotCode = r.ParkingSlot != null ? r.ParkingSlot.SlotCode : "",
                    Status = r.Status,
                    ReservationReference = r.ReservationReference,
                    PaymentReference = r.PaymentReference,
                    PaymentAmount = r.PaymentAmount,
                    ReservedAt = r.ReservedAt,
                    CheckInAt = r.CheckInAt,
                    CheckOutAt = r.CheckOutAt,
                    Latitude = r.ParkingSlot != null && r.ParkingSlot.ParkingLocation != null
                             ? r.ParkingSlot.ParkingLocation.Latitude
                             : 0,
                    Longitude = r.ParkingSlot != null && r.ParkingSlot.ParkingLocation != null
                              ? r.ParkingSlot.ParkingLocation.Longitude
                              : 0
                })
                .FirstOrDefaultAsync();

            if (reservation == null)
                return NotFound(new { message = "No active parking found." });

            return Ok(reservation);
        }

        [HttpPost("done-parking")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DoneParking([FromBody] DoneParkingDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r => r.Id == dto.ReservationId && r.UserId == userId);

            if (reservation == null)
                return NotFound(new { message = "Reservation not found." });

            if (reservation.Status != "Occupied")
                return BadRequest(new { message = "Only occupied parking can proceed to payment." });

            if (string.IsNullOrWhiteSpace(reservation.PaymentReference))
            {
                reservation.PaymentReference = GeneratePaymentReference();
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = "Payment QR is ready.",
                paymentReference = reservation.PaymentReference,
                reservationReference = reservation.ReservationReference,
                amount = reservation.PaymentAmount
            });
        }

        [HttpPost("admin/scan-arrival")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> ScanArrival([FromBody] AdminReferenceActionDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int adminUserId))
                return Unauthorized();

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || adminUser.ParkingLocationId == null)
                return BadRequest(new { message = "Admin parking location not found." });

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r => r.ReservationReference == dto.Reference);

            if (reservation == null)
                return NotFound(new { message = "Reservation not found." });

            if (reservation.ParkingSlot == null || reservation.ParkingSlot.ParkingLocationId != adminUser.ParkingLocationId.Value)
                return BadRequest(new { message = "Reservation does not belong to your location." });

            if (reservation.Status != "Reserved")
                return BadRequest(new { message = "Reservation is not in reserved state." });

            reservation.Status = "Occupied";
            reservation.CheckInAt = DateTime.UtcNow;
            reservation.ParkingSlot.Status = "Occupied";

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Arrival confirmed." });
        }

        [HttpPost("admin/scan-payment")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> ScanPayment([FromBody] AdminReferenceActionDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int adminUserId))
                return Unauthorized();

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || adminUser.ParkingLocationId == null)
                return BadRequest(new { message = "Admin parking location not found." });

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r => r.PaymentReference == dto.Reference);

            if (reservation == null)
                return NotFound(new { message = "Payment reference not found." });

            if (reservation.ParkingSlot == null || reservation.ParkingSlot.ParkingLocationId != adminUser.ParkingLocationId.Value)
                return BadRequest(new { message = "Payment does not belong to your location." });

            if (reservation.Status != "Occupied")
                return BadRequest(new { message = "Reservation is not occupied." });

            reservation.CheckOutAt = DateTime.UtcNow;
            reservation.PaidAt = DateTime.UtcNow;
            reservation.PaymentMethod = "Cash";
            reservation.PaymentStatus = "Paid";
            reservation.Status = "Completed";
            reservation.ParkingSlot.Status = "Available";

            _context.ParkingHistories.Add(new Models.ParkingHistory
            {
                UserId = reservation.UserId,
                ParkingSlotId = reservation.ParkingSlotId,
                ParkingLocationId = reservation.ParkingSlot.ParkingLocationId,
                ReservationReference = reservation.ReservationReference,
                PaymentReference = reservation.PaymentReference,
                ReservedAt = reservation.ReservedAt,
                CheckInAt = reservation.CheckInAt,
                CheckOutAt = reservation.CheckOutAt,
                PaidAt = reservation.PaidAt,
                PaymentAmount = reservation.PaymentAmount,
                PaymentMethod = reservation.PaymentMethod,
                PaymentStatus = reservation.PaymentStatus,
                Status = reservation.Status
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Payment completed and slot is now available." });
        }

        [HttpPost("admin/manual-arrive")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> ManualArrive([FromBody] ManualSlotActionDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int adminUserId))
                return Unauthorized();

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || adminUser.ParkingLocationId == null)
                return BadRequest(new { message = "Admin parking location not found." });

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .Include(r => r.User)
                .Where(r => r.ParkingSlotId == dto.ParkingSlotId && r.Status == "Reserved")
                .OrderByDescending(r => r.ReservedAt)
                .FirstOrDefaultAsync();

            if (reservation == null)
                return NotFound(new { message = "No reserved user found for this slot." });

            if (reservation.ParkingSlot == null || reservation.ParkingSlot.ParkingLocationId != adminUser.ParkingLocationId.Value)
                return BadRequest(new { message = "Slot does not belong to your location." });

            reservation.Status = "Occupied";
            reservation.CheckInAt = DateTime.UtcNow;
            reservation.ParkingSlot.Status = "Occupied";

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Manual arrival confirmed." });
        }

        [HttpPost("admin/manual-checkout")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> ManualCheckout([FromBody] ManualSlotActionDto dto)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdClaim, out int adminUserId))
                return Unauthorized();

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || adminUser.ParkingLocationId == null)
                return BadRequest(new { message = "Admin parking location not found." });

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .Where(r => r.ParkingSlotId == dto.ParkingSlotId && r.Status == "Occupied")
                .OrderByDescending(r => r.CheckInAt)
                .FirstOrDefaultAsync();

            if (reservation == null)
                return NotFound(new { message = "No occupied reservation found for this slot." });

            if (reservation.ParkingSlot == null || reservation.ParkingSlot.ParkingLocationId != adminUser.ParkingLocationId.Value)
                return BadRequest(new { message = "Slot does not belong to your location." });

            reservation.CheckOutAt = DateTime.UtcNow;
            reservation.PaidAt = DateTime.UtcNow;
            reservation.PaymentMethod = "Cash";
            reservation.PaymentStatus = "Paid";
            reservation.Status = "Completed";
            reservation.ParkingSlot.Status = "Available";

            _context.ParkingHistories.Add(new Models.ParkingHistory
            {
                UserId = reservation.UserId,
                ParkingSlotId = reservation.ParkingSlotId,
                ParkingLocationId = reservation.ParkingSlot.ParkingLocationId,
                ReservationReference = reservation.ReservationReference,
                PaymentReference = reservation.PaymentReference,
                ReservedAt = reservation.ReservedAt,
                CheckInAt = reservation.CheckInAt,
                CheckOutAt = reservation.CheckOutAt,
                PaidAt = reservation.PaidAt,
                PaymentAmount = reservation.PaymentAmount,
                PaymentMethod = reservation.PaymentMethod,
                PaymentStatus = reservation.PaymentStatus,
                Status = reservation.Status
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Manual checkout completed." });
        }

        [HttpGet("admin/slot-details/{slotId}")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> GetSlotDetails(int slotId)
        {
            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null)
                return NotFound();

            var reservation = await _context.ParkingReservations
                .Include(r => r.User)
                .Where(r => r.ParkingSlotId == slotId &&
                       (r.Status == "Reserved" || r.Status == "Occupied"))
                .OrderByDescending(r => r.ReservedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                slot.Id,
                slot.SlotCode,
                slot.Status,
                slot.IsActive,
                ReservedUser = reservation != null ? reservation.User!.Username : null,
                reservation?.ReservedAt,
                reservation?.CheckInAt,
                ReservationReference = reservation != null ? reservation.ReservationReference : null,
                PaymentReference = reservation != null ? reservation.PaymentReference : null
            });
        }



        private string GenerateReservationReference()
        {
            return $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        private string GeneratePaymentReference()
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }
    }
}