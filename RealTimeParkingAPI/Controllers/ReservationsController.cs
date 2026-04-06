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

            if (!slot.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Slot is not available." });

            var existingReservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r =>
                    r.UserId == userId &&
                    (r.Status == "Reserved" || r.Status == "Occupied"));

            if (existingReservation != null)
                return BadRequest(new { message = "You already have an active reservation." });

            var paymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod)
                ? "Cash"
                : dto.PaymentMethod.Trim();

            var reservation = new ParkingReservation
            {
                UserId = userId,
                ParkingSlotId = dto.ParkingSlotId,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Status = "Reserved",
                ReservationReference = GenerateReservationReference(),
                PaymentAmount = 20m,

                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod.Equals("GCash", StringComparison.OrdinalIgnoreCase)
                    ? "Pending"
                    : "Unpaid",

                HasArrived = false
            };

            slot.Status = "Reserved";

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Reservation created successfully.",
                reservationId = reservation.Id,
                reservationReference = reservation.ReservationReference,
                paymentMethod = reservation.PaymentMethod,
                paymentStatus = reservation.PaymentStatus
            });
        }

        [HttpGet("my-active")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyActiveReservation()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var activeStatuses = new[]
            {
        "Reserved",
        "Occupied",
        "PendingCashConfirmation",
        "PendingGcashConfirmation"
    };

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .Where(r => r.UserId == userId && activeStatuses.Contains(r.Status))
                .OrderByDescending(r => r.ReservedAt)
                .FirstOrDefaultAsync();

            if (reservation == null || reservation.ParkingSlot == null || reservation.ParkingSlot.ParkingLocation == null)
                return NotFound(new { message = "No active parking found." });

            var dto = new ActiveParkingDto
            {
                ReservationId = reservation.Id,
                ParkingSlotId = reservation.ParkingSlotId,
                ParkingLocationId = reservation.ParkingSlot.ParkingLocationId,
                Latitude = reservation.ParkingSlot.ParkingLocation.Latitude,
                Longitude = reservation.ParkingSlot.ParkingLocation.Longitude,
                ParkingLocationName = reservation.ParkingSlot.ParkingLocation.Name,
                SlotCode = reservation.ParkingSlot.SlotCode,
                Status = reservation.Status,
                ReservationReference = reservation.ReservationReference,
                PaymentReference = reservation.PaymentReference,
                PaymentAmount = reservation.PaymentAmount,
                PaymentMethod = reservation.PaymentMethod,
                PaymentStatus = reservation.PaymentStatus,
                ReservedAt = reservation.ReservedAt,
                CheckInAt = reservation.CheckInAt,
                CheckOutAt = reservation.CheckOutAt,
                HasArrived = reservation.HasArrived,
                CanGenerateArrivalQr = reservation.HasArrived && reservation.Status == "Reserved",
                CanGeneratePaymentQr =
                    reservation.Status == "Occupied" &&
                    string.Equals(reservation.PaymentMethod, "GCash", StringComparison.OrdinalIgnoreCase)
            };

            return Ok(dto);
        }

        [HttpPost("mark-arrived/{reservationId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MarkArrived(int reservationId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null)
                return NotFound(new { message = "Reservation not found." });

            if (reservation.Status != "Reserved")
                return BadRequest(new { message = "Only reserved parking can be marked as arrived." });

            reservation.HasArrived = true;
            reservation.ArrivedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Arrival marked successfully." });
        }

        [HttpPost("create-payment-request/{reservationId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreatePaymentRequest(int reservationId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                    .ThenInclude(s => s!.ParkingLocation)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation == null || reservation.ParkingSlot == null || reservation.ParkingSlot.ParkingLocation == null)
                return NotFound(new { message = "Reservation not found." });

            if (!reservation.HasArrived)
                return BadRequest(new { message = "You must arrive first before generating payment QR." });

            if (!string.Equals(reservation.PaymentMethod, "GCash", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Payment QR is only available for GCash payment." });

            var existingPayment = await _context.PaymentRequests
                .FirstOrDefaultAsync(p => p.ReservationId == reservation.Id && p.Status == "Pending");

            if (existingPayment != null)
            {
                return Ok(existingPayment);
            }

            var location = reservation.ParkingSlot.ParkingLocation;

            var paymentReference = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{reservation.Id}";

            var paymentRequest = new PaymentRequest
            {
                ReservationId = reservation.Id,
                UserId = userId,
                ParkingLocationId = location.Id,
                Provider = "GCash",
                PaymentReference = paymentReference,
                Amount = reservation.PaymentAmount,
                Status = "Pending",
                ExternalPaymentUrl = location.MerchantPaymentUrl,
                MerchantDisplayName = location.MerchantDisplayName,
                MerchantGcashNumber = location.MerchantGcashNumber,
                CreatedAt = DateTime.UtcNow
            };

            reservation.PaymentReference = paymentReference;
            reservation.Status = "Occupied";
            reservation.PaymentStatus = "Pending";

            _context.PaymentRequests.Add(paymentRequest);
            await _context.SaveChangesAsync();

            return Ok(paymentRequest);
        }

        [HttpPut("done-parking")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DoneParking(DoneParkingDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r => r.Id == dto.ReservationId && r.UserId == userId);

            if (reservation == null || reservation.ParkingSlot == null)
                return NotFound(new { message = "Reservation not found." });

            if (reservation.Status != "Occupied")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only occupied reservations can proceed to done parking."
                });
            }

            var paymentMethod = string.IsNullOrWhiteSpace(reservation.PaymentMethod)
                ? "Cash"
                : reservation.PaymentMethod.Trim();

            reservation.PaymentMethod = paymentMethod;

            if (paymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
            {
                reservation.Status = "PendingCashConfirmation";
                reservation.PaymentStatus = "Unpaid";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Waiting for cash confirmation.",
                    reservation.Id,
                    reservation.Status,
                    reservation.PaymentStatus,
                    reservation.PaymentMethod
                });
            }

            if (string.IsNullOrWhiteSpace(reservation.PaymentReference))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Generate payment request first."
                });
            }

            reservation.Status = "PendingGcashConfirmation";
            reservation.PaymentStatus = "Pending";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Waiting for GCash confirmation.",
                reservation.Id,
                reservation.Status,
                reservation.PaymentStatus,
                reservation.PaymentMethod
            });
        }

        [HttpPut("cash-checkout/{slotId}")]
        [Authorize(Roles = "LocationAdmin,SuperAdmin")]
        public async Task<IActionResult> CashCheckout(int slotId)
        {
            var slot = await _context.ParkingSlots
                .Include(s => s.ParkingLocation)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Slot not found."
                });
            }

            var reservation = await _context.ParkingReservations
                .FirstOrDefaultAsync(r =>
                    r.ParkingSlotId == slotId &&
                    (r.Status == "Occupied" || r.Status == "PendingCashConfirmation"));

            if (reservation == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No cash reservation found for this slot."
                });
            }

            if (string.IsNullOrWhiteSpace(reservation.PaymentReference))
            {
                reservation.PaymentReference = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{reservation.Id}";
            }

            reservation.CheckOutAt = DateTime.UtcNow;
            reservation.PaidAt = DateTime.UtcNow;
            reservation.Status = "Completed";
            reservation.PaymentStatus = "Paid";

            slot.Status = "Available";

            _context.ParkingHistories.Add(new ParkingHistory
            {
                UserId = reservation.UserId,
                ParkingSlotId = reservation.ParkingSlotId,
                ParkingLocationId = slot.ParkingLocationId,
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

            return Ok(new
            {
                success = true,
                message = "Cash checkout successful."
            });
        }

        [HttpGet("my-location-reservations")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> GetMyLocationReservations()
        {
            var locationIdClaim = User.FindFirst("ParkingLocationId")?.Value;
            if (!int.TryParse(locationIdClaim, out int parkingLocationId))
                return Unauthorized(new { message = "Location admin is not assigned to a parking location." });

            var reservations = await _context.ParkingReservations
                .Include(r => r.User)
                .Include(r => r.ParkingSlot)
                .Where(r => r.ParkingSlot != null && r.ParkingSlot.ParkingLocationId == parkingLocationId)
                .OrderByDescending(r => r.ReservedAt)
                .Select(r => new
                {
                    r.Id,
                    Username = r.User != null ? r.User.Username : "Unknown",
                    SlotCode = r.ParkingSlot != null ? r.ParkingSlot.SlotCode : "N/A",
                    r.ReservationReference,
                    r.PaymentReference,
                    r.Status,
                    r.PaymentMethod,
                    r.PaymentStatus,
                    r.ReservedAt,
                    r.CheckInAt,
                    r.CheckOutAt,
                    r.PaidAt,
                    r.PaymentAmount,
                    r.HasArrived
                })
                .ToListAsync();

            return Ok(reservations);
        }

        [HttpGet("admin/slot-details/{slotId}")]
        [Authorize(Roles = "LocationAdmin,SuperAdmin")]
        public async Task<IActionResult> GetAdminSlotDetails(int slotId)
        {
            var slot = await _context.ParkingSlots
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            var activeStatuses = new[]
            {
        "Reserved",
        "Occupied",
        "PendingCashConfirmation",
        "PendingGcashConfirmation"
        };

            var reservation = await _context.ParkingReservations
                .Include(r => r.User)
                .Where(r => r.ParkingSlotId == slotId && activeStatuses.Contains(r.Status))
                .OrderByDescending(r => r.ReservedAt)
                .FirstOrDefaultAsync();

            var dto = new SlotDetailsDto
            {
                SlotId = slot.Id,
                SlotCode = slot.SlotCode,
                Status = reservation?.Status ?? slot.Status,
                ReservedUser = reservation?.User != null ? reservation.User.Username : null,
                ReservedAt = reservation?.ReservedAt,
                CheckInAt = reservation?.CheckInAt,
                ReservationReference = reservation?.ReservationReference,
                PaymentReference = reservation?.PaymentReference,
                PaymentMethod = reservation?.PaymentMethod,
                PaymentStatus = reservation?.PaymentStatus
            };

            return Ok(dto);
        }

        [HttpPost("admin/manual-arrive")]
        [Authorize(Roles = "LocationAdmin,SuperAdmin")]
        public async Task<IActionResult> ManualArrive([FromBody] ManualSlotActionDto dto)
        {
            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r => r.ParkingSlotId == dto.ParkingSlotId && r.Status == "Reserved");

            if (reservation == null || reservation.ParkingSlot == null)
                return NotFound(new { success = false, message = "No reserved parking found for this slot." });

            reservation.HasArrived = true;
            reservation.ArrivedAt = DateTime.UtcNow;
            reservation.CheckInAt = DateTime.UtcNow;
            reservation.Status = "Occupied";
            reservation.ParkingSlot.Status = "Occupied";

            if (string.IsNullOrWhiteSpace(reservation.PaymentMethod))
                reservation.PaymentMethod = "Cash";

            if (reservation.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                reservation.PaymentStatus = "Unpaid";
            else
                reservation.PaymentStatus = "Pending";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Arrival confirmed successfully.",
                reservationId = reservation.Id,
                status = reservation.Status,
                slotStatus = reservation.ParkingSlot.Status,
                checkInAt = reservation.CheckInAt
            });
        }

        [HttpPost("admin/manual-checkout")]
        [Authorize(Roles = "LocationAdmin,SuperAdmin")]
        public async Task<IActionResult> ManualCheckout([FromBody] ManualSlotActionDto dto)
        {
            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r =>
                    r.ParkingSlotId == dto.ParkingSlotId &&
                    (r.Status == "Occupied" || r.Status == "PendingGcashConfirmation"));

            if (reservation == null || reservation.ParkingSlot == null)
                return NotFound(new { success = false, message = "No active parking found for this slot." });

            reservation.CheckOutAt = DateTime.UtcNow;
            reservation.Status = "Completed";
            reservation.PaymentStatus = "Paid";
            reservation.PaidAt ??= DateTime.UtcNow;
            reservation.ParkingSlot.Status = "Available";

            _context.ParkingHistories.Add(new ParkingHistory
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

            return Ok(new { success = true, message = "Manual checkout completed successfully." });
        }

        [HttpPost("admin/scan-arrival")]
        [Authorize(Roles = "LocationAdmin,SuperAdmin")]
        public async Task<IActionResult> ScanArrival([FromBody] AdminReferenceActionDto dto)
        {
            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r =>
                    r.ReservationReference == dto.Reference &&
                    r.Status == "Reserved");

            if (reservation == null || reservation.ParkingSlot == null)
                return NotFound(new { success = false, message = "Reservation reference not found." });

            reservation.HasArrived = true;
            reservation.ArrivedAt = DateTime.UtcNow;
            reservation.CheckInAt = DateTime.UtcNow;
            reservation.Status = "Occupied";
            reservation.ParkingSlot.Status = "Occupied";

            if (string.IsNullOrWhiteSpace(reservation.PaymentMethod))
                reservation.PaymentMethod = "Cash";

            if (reservation.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                reservation.PaymentStatus = "Unpaid";
            else
                reservation.PaymentStatus = "Pending";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Arrival QR scanned successfully.",
                reservationId = reservation.Id,
                status = reservation.Status,
                slotStatus = reservation.ParkingSlot.Status,
                checkInAt = reservation.CheckInAt
            });
        }

        [HttpPost("admin/scan-payment")]
        [Authorize(Roles = "LocationAdmin,SuperAdmin")]
        public async Task<IActionResult> ScanPayment([FromBody] AdminReferenceActionDto dto)
        {
            var reservation = await _context.ParkingReservations
                .Include(r => r.ParkingSlot)
                .FirstOrDefaultAsync(r =>
                    r.PaymentReference == dto.Reference &&
                    (r.Status == "Occupied" || r.Status == "PendingGcashConfirmation"));

            if (reservation == null || reservation.ParkingSlot == null)
                return NotFound(new { success = false, message = "Payment reference not found." });

            reservation.PaymentStatus = "Paid";
            reservation.PaidAt = DateTime.UtcNow;
            reservation.CheckOutAt = DateTime.UtcNow;
            reservation.Status = "Completed";
            reservation.ParkingSlot.Status = "Available";

            _context.ParkingHistories.Add(new ParkingHistory
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

            return Ok(new { success = true, message = "Payment QR scanned successfully." });
        }

        [HttpGet("my-history")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var history = await _context.ParkingHistories
                .Include(h => h.ParkingSlot)
                .Include(h => h.ParkingLocation)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CheckOutAt ?? h.ReservedAt)
                .Select(h => new
                {
                    h.Id,
                    h.ReservationReference,
                    h.PaymentReference,
                    ParkingLocationName = h.ParkingLocation != null ? h.ParkingLocation.Name : "Unknown",
                    SlotCode = h.ParkingSlot != null ? h.ParkingSlot.SlotCode : "N/A",
                    h.ReservedAt,
                    h.CheckInAt,
                    h.CheckOutAt,
                    h.PaidAt,
                    h.PaymentAmount,
                    h.PaymentMethod,
                    h.PaymentStatus,
                    h.Status
                })
                .ToListAsync();

            return Ok(history);
        }

        private string GenerateReservationReference()
        {
            return $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        }
    }
}