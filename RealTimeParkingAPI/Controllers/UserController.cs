using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.Models;

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

        // Helper to hash password
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // Trim inputs
            user.Username = user.Username?.Trim();
            user.Email = user.Email?.Trim();
            user.FirstName = user.FirstName?.Trim();
            user.LastName = user.LastName?.Trim();
            user.MiddleName = user.MiddleName?.Trim();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.PasswordHash) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.FirstName) ||
                string.IsNullOrWhiteSpace(user.LastName))
            {
                return BadRequest("Username, First Name, Last Name, Email, and Password are required.");
            }

            // Normalize for comparison
            var normalizedUsername = user.Username.ToLower();
            var normalizedEmail = user.Email.ToLower();

            // Check duplicates
            if (await _context.Users.AnyAsync(u =>
                    u.Username.ToLower() == normalizedUsername ||
                    u.Email.ToLower() == normalizedEmail))
            {
                return BadRequest("Username or Email already exists.");
            }

            var rawPassword = user.PasswordHash; // keep raw password for auto-login
            user.PasswordHash = HashPassword(rawPassword);

            // Set defaults
            user.Role = "User";
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Auto-login
            var tokenResponse = await Login(new LoginRequest
            {
                UsernameOrEmail = user.Username,
                Password = rawPassword // raw password
            });

            return Ok(new
            {
                message = "Registration successful",
                token = ((dynamic)tokenResponse).Value.token,
                role = ((dynamic)tokenResponse).Value.role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == request.UsernameOrEmail.ToLower() ||
                    u.Username.ToLower() == request.UsernameOrEmail.ToLower());

            if (user == null)
                return Unauthorized("User not found");

            // Hash input password and compare
            var hashedInput = HashPassword(request.Password);
            if (user.PasswordHash != hashedInput)
                return Unauthorized("Invalid password");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = jwt,
                role = user.Role
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            // Hash password before saving
            user.PasswordHash = HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }
    }
}