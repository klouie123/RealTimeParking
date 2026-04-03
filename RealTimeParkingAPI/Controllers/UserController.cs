using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            user.Username = user.Username?.Trim();
            user.Email = user.Email?.Trim();
            user.FirstName = user.FirstName?.Trim();
            user.LastName = user.LastName?.Trim();
            user.MiddleName = user.MiddleName?.Trim();

            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.PasswordHash) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.FirstName) ||
                string.IsNullOrWhiteSpace(user.LastName))
            {
                return BadRequest("Username, First Name, Last Name, Email, and Password are required.");
            }

            var normalizedUsername = user.Username.ToLower();
            var normalizedEmail = user.Email.ToLower();

            if (await _context.Users.AnyAsync(u =>
                    u.Username.ToLower() == normalizedUsername ||
                    u.Email.ToLower() == normalizedEmail))
            {
                return BadRequest("Username or Email already exists.");
            }

            var rawPassword = user.PasswordHash;
            user.PasswordHash = HashPassword(rawPassword);

            user.Role = "User";
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await Login(new LoginRequest
            {
                UsernameOrEmail = user.Username,
                Password = rawPassword
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var normalizedInput = request.UsernameOrEmail.Trim().ToLower();

            var user = await _context.Users
                .Include(u => u.ParkingLocation)
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == normalizedInput ||
                    u.Username.ToLower() == normalizedInput);

            if (user == null)
                return Unauthorized("User not found");

            var hashedInput = HashPassword(request.Password);
            if (user.PasswordHash != hashedInput)
                return Unauthorized("Invalid password");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            if (user.ParkingLocationId.HasValue)
            {
                claims.Add(new Claim("ParkingLocationId", user.ParkingLocationId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(12),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                isSuccess = true,
                userId = user.Id,
                token = jwt,
                role = user.Role,
                username = user.Username,
                email = user.Email,
                parkingLocationId = user.ParkingLocationId,
                parkingLocationName = user.ParkingLocation != null ? user.ParkingLocation.Name : null
            });
        }

        [HttpGet("my-history")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var history = await _context.ParkingHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ReservedAt)
                .Select(h => new
                {
                    h.Id,
                    ParkingLocationName = h.ParkingLocation != null ? h.ParkingLocation.Name : "Parking Location",
                    SlotCode = h.ParkingSlot != null ? h.ParkingSlot.SlotCode : "N/A",
                    h.ReservationReference,
                    h.PaymentReference,
                    h.ReservedAt,
                    h.CheckInAt,
                    h.CheckOutAt,
                    h.PaymentAmount,
                    h.Status
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.ParkingLocation)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.ParkingLocationId,
                    ParkingLocationName = u.ParkingLocation != null ? u.ParkingLocation.Name : null,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.ParkingLocation)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                user.Email,
                user.Role,
                user.ParkingLocationId,
                ParkingLocationName = user.ParkingLocation != null ? user.ParkingLocation.Name : null,
                user.CreatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            user.PasswordHash = HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }
    }
}