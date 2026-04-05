using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.DTOs;
using RealTimeParkingAPI.Helpers;
using RealTimeParkingAPI.Models;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return null;

            return await _context.Users
                .Include(u => u.ParkingLocation)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        [HttpPost("create-super-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSuperAdmin()
        {
            var existing = await _context.Users.FirstOrDefaultAsync(x => x.Role == "SuperAdmin");
            if (existing != null)
                return BadRequest(new { message = "SuperAdmin already exists." });

            var user = new User
            {
                Username = "superadmin",
                FirstName = "Main",
                LastName = "Admin",
                Email = "superadmin@gmail.com",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Role = "SuperAdmin",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "SuperAdmin created successfully." });
        }

        [HttpGet("super-dashboard")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetSuperDashboard()
        {
            var totalLocations = await _context.ParkingLocations.CountAsync();
            var totalLocationAdmins = await _context.Users.CountAsync(u => u.Role == "LocationAdmin");
            var totalUsers = await _context.Users.CountAsync(u => u.Role == "User");
            var totalSlots = await _context.ParkingSlots.CountAsync();
            var totalReservations = await _context.ParkingReservations.CountAsync();

            return Ok(new
            {
                totalLocations,
                totalLocationAdmins,
                totalUsers,
                totalSlots,
                totalReservations
            });
        }

        [HttpPost("create-parking-location")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateParkingLocation(CreateParkingLocationDto dto)
        {
            var location = new ParkingLocation
            {
                Name = dto.Name,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                AllowedMinutes = dto.AllowedMinutes,
                Description = dto.Description
            };

            _context.ParkingLocations.Add(location);
            await _context.SaveChangesAsync();

            return Ok(location);
        }

        [HttpGet("parking-locations")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetParkingLocations()
        {
            var locations = await _context.ParkingLocations
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Ok(locations);
        }

        [HttpPost("create-location-admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateLocationAdmin(CreateLocationAdminDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Password is required." });

            if (dto.Password.Length < 6)
                return BadRequest(new { message = "Password too weak." });

            var location = await _context.ParkingLocations.FindAsync(dto.ParkingLocationId);
            if (location == null)
                return BadRequest(new { message = "Parking location not found." });

            var exists = await _context.Users.AnyAsync(x =>
                x.Username == dto.Username || x.Email == dto.Email);

            if (exists)
                return BadRequest(new { message = "Username or email already exists." });

            var user = new User
            {
                Username = dto.Username.Trim(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                Role = "LocationAdmin",
                ParkingLocationId = dto.ParkingLocationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Location admin created successfully." });
        }

        [HttpGet("location-admins")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetLocationAdmins()
        {
            var admins = await _context.Users
                .Include(u => u.ParkingLocation)
                .Where(u => u.Role == "LocationAdmin")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.ParkingLocationId,
                    ParkingLocationName = u.ParkingLocation != null ? u.ParkingLocation.Name : null,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(admins);
        }

        [HttpPost("create-slot")]
        [Authorize(Roles = "SuperAdmin,LocationAdmin")]
        public async Task<IActionResult> CreateSlot(CreateParkingSlotDto dto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.Role == "LocationAdmin" &&
                currentUser.ParkingLocationId != dto.ParkingLocationId)
            {
                return Forbid();
            }

            var location = await _context.ParkingLocations.FindAsync(dto.ParkingLocationId);
            if (location == null)
                return BadRequest(new { message = "Parking location not found." });

            var exists = await _context.ParkingSlots.AnyAsync(s =>
                s.ParkingLocationId == dto.ParkingLocationId &&
                s.SlotCode == dto.SlotCode);

            if (exists)
                return BadRequest(new { message = "Slot code already exists in this location." });

            var slot = new ParkingSlot
            {
                ParkingLocationId = dto.ParkingLocationId,
                SlotCode = dto.SlotCode.Trim(),
                Status = "Available",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ParkingSlots.Add(slot);
            await _context.SaveChangesAsync();

            return Ok(slot);
        }

        [HttpGet("my-location-dashboard")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> GetMyLocationDashboard()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.ParkingLocationId == null)
                return BadRequest(new { message = "This admin is not assigned to a parking location." });

            var locationId = currentUser.ParkingLocationId.Value;

            var totalSlots = await _context.ParkingSlots
                .CountAsync(x => x.ParkingLocationId == locationId && x.IsActive);

            var availableSlots = await _context.ParkingSlots
                .CountAsync(x => x.ParkingLocationId == locationId && x.IsActive && x.Status == "Available");

            var reservedSlots = await _context.ParkingSlots
                .CountAsync(x => x.ParkingLocationId == locationId && x.IsActive && x.Status == "Reserved");

            var occupiedSlots = await _context.ParkingSlots
                .CountAsync(x => x.ParkingLocationId == locationId && x.IsActive && x.Status == "Occupied");

            var activeReservations = await _context.ParkingReservations
                .CountAsync(r => r.ParkingSlot != null &&
                     r.ParkingSlot.ParkingLocationId == locationId &&
                     (r.Status == "Reserved" || r.Status == "Occupied"));

            return Ok(new LocationAdminDashboardDto
            {
                ParkingLocationName = currentUser.ParkingLocation?.Name ?? "",
                TotalSlots = totalSlots,
                AvailableSlots = availableSlots,
                ReservedSlots = reservedSlots,
                OccupiedSlots = occupiedSlots,
                ActiveReservations = activeReservations
            });
        }

        [HttpGet("my-location-slots")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> GetMyLocationSlots()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.ParkingLocationId == null)
                return BadRequest("This admin is not assigned to a parking location.");

            var slots = await _context.ParkingSlots
                .Where(x => x.ParkingLocationId == currentUser.ParkingLocationId.Value)
                .OrderBy(x => x.SlotCode)
                .ToListAsync();

            return Ok(slots);
        }

        [HttpPut("slot-status/{slotId}")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> UpdateSlotStatus(int slotId, [FromBody] UpdateSlotStatusDto dto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.ParkingLocationId == null)
                return BadRequest(new { message = "This admin is not assigned to a parking location." });

            var slot = await _context.ParkingSlots.FirstOrDefaultAsync(x => x.Id == slotId);
            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            if (slot.ParkingLocationId != currentUser.ParkingLocationId.Value)
                return Forbid();

            var allowedStatuses = new[] { "Available", "Reserved", "Occupied" };
            if (!allowedStatuses.Contains(dto.Status))
                return BadRequest(new { message = "Invalid status." });

            slot.Status = dto.Status;
            slot.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Slot updated successfully." });
        }

        [HttpGet("location-maintenance")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> GetLocationMaintenance()
        {
            var locationIdClaim = User.FindFirst("ParkingLocationId")?.Value;
            if (!int.TryParse(locationIdClaim, out int parkingLocationId))
                return Unauthorized(new { message = "Location admin is not assigned to a parking location." });

            var location = await _context.ParkingLocations
                .Include(p => p.ParkingSlots)
                .FirstOrDefaultAsync(p => p.Id == parkingLocationId);

            if (location == null)
                return NotFound(new { message = "Parking location not found." });

            var dto = new LocationMaintenanceDto
            {
                ParkingPrice = location.ParkingPrice,
                Slots = location.ParkingSlots
                    .OrderBy(s => s.SlotCode)
                    .Select(s => new LocationMaintenanceSlotDto
                    {
                        Id = s.Id,
                        SlotCode = s.SlotCode,
                        EditableSlotCode = s.SlotCode,
                        Status = s.Status,
                        IsActive = s.IsActive
                    })
                    .ToList()
            };

            return Ok(dto);
        }

        [HttpPut("update-parking-price")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> UpdateParkingPrice([FromBody] UpdateParkingPriceDto dto)
        {
            var locationIdClaim = User.FindFirst("ParkingLocationId")?.Value;
            if (!int.TryParse(locationIdClaim, out int parkingLocationId))
                return Unauthorized(new { message = "Location admin is not assigned to a parking location." });

            var location = await _context.ParkingLocations.FirstOrDefaultAsync(p => p.Id == parkingLocationId);
            if (location == null)
                return NotFound(new { message = "Parking location not found." });

            location.ParkingPrice = dto.ParkingPrice;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Parking price updated successfully." });
        }

        [HttpPost("add-location-slot")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> AddLocationSlot([FromBody] AddLocationSlotDto dto)
        {
            var locationIdClaim = User.FindFirst("ParkingLocationId")?.Value;
            if (!int.TryParse(locationIdClaim, out int parkingLocationId))
                return Unauthorized(new { message = "Location admin is not assigned to a parking location." });

            if (string.IsNullOrWhiteSpace(dto.SlotCode))
                return BadRequest(new { message = "Slot code is required." });

            var exists = await _context.ParkingSlots.AnyAsync(s =>
                s.ParkingLocationId == parkingLocationId &&
                s.SlotCode == dto.SlotCode);

            if (exists)
                return BadRequest(new { message = "Slot code already exists." });

            var slot = new ParkingSlot
            {
                ParkingLocationId = parkingLocationId,
                SlotCode = dto.SlotCode.Trim(),
                Status = "Available",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.ParkingSlots.Add(slot);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Slot added successfully." });
        }

        [HttpPut("rename-slot/{slotId}")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> RenameSlot(int slotId, [FromBody] RenameSlotDto dto)
        {
            var locationIdClaim = User.FindFirst("ParkingLocationId")?.Value;
            if (!int.TryParse(locationIdClaim, out int parkingLocationId))
                return Unauthorized(new { message = "Location admin is not assigned to a parking location." });

            var slot = await _context.ParkingSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ParkingLocationId == parkingLocationId);

            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            if (string.IsNullOrWhiteSpace(dto.SlotCode))
                return BadRequest(new { message = "Slot code is required." });

            var exists = await _context.ParkingSlots.AnyAsync(s =>
                s.ParkingLocationId == parkingLocationId &&
                s.Id != slotId &&
                s.SlotCode == dto.SlotCode);

            if (exists)
                return BadRequest(new { message = "Another slot already uses this code." });

            slot.SlotCode = dto.SlotCode.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Slot renamed successfully." });
        }

        [HttpPut("toggle-slot-active/{slotId}")]
        [Authorize(Roles = "LocationAdmin")]
        public async Task<IActionResult> ToggleSlotActive(int slotId, [FromBody] ToggleSlotActiveDto dto)
        {
            var locationIdClaim = User.FindFirst("ParkingLocationId")?.Value;
            if (!int.TryParse(locationIdClaim, out int parkingLocationId))
                return Unauthorized(new { message = "Location admin is not assigned to a parking location." });

            var slot = await _context.ParkingSlots
                .FirstOrDefaultAsync(s => s.Id == slotId && s.ParkingLocationId == parkingLocationId);

            if (slot == null)
                return NotFound(new { message = "Slot not found." });

            slot.IsActive = dto.IsActive;

            if (!slot.IsActive && slot.Status == "Available")
                slot.Status = "Inactive";

            if (slot.IsActive && slot.Status == "Inactive")
                slot.Status = "Available";

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Slot status updated successfully." });
        }

        //[HttpGet("my-location-reservations")]
        //[Authorize(Roles = "LocationAdmin")]
        //public async Task<IActionResult> GetMyLocationReservations()
        //{
        //    var currentUser = await GetCurrentUserAsync();
        //    if (currentUser == null)
        //        return Unauthorized();

        //    if (currentUser.ParkingLocationId == null)
        //        return BadRequest(new { message = "This admin is not assigned to a parking location." });

        //    var locationId = currentUser.ParkingLocationId.Value;

        //    var reservations = await _context.ParkingReservations
        //        .Include(r => r.User)
        //        .Include(r => r.ParkingSlot)
        //        .Where(r => r.ParkingSlot != null && r.ParkingSlot.ParkingLocationId == locationId)
        //        .Select(r => new
        //        {
        //            r.Id,
        //            r.UserId,
        //            Username = r.User != null ? r.User.Username : "",
        //            SlotCode = r.ParkingSlot != null ? r.ParkingSlot.SlotCode : "",
        //            r.ReservedAt,
        //            r.Status
        //        })
        //        .OrderByDescending(r => r.ReservedAt)
        //        .ToListAsync();

        //    return Ok(reservations);
        //}
    }
}