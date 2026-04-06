using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealTimeParkingAPI.Data;
using RealTimeParkingAPI.DTOs;
using RealTimeParkingAPI.Helpers;
using RealTimeParkingAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RealTimeParkingAPI.Services;

namespace RealTimeParkingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public UserController(AppDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            return user;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            if (!IsStrongPassword(dto.Password))
            {
                return BadRequest(new
                {
                    message = "Password must be at least 8 characters, include uppercase, lowercase, number, and special character."
                });
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest(new { message = "Username already exists." });

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already exists." });

            var user = new User
            {
                Username = dto.Username,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                Role = "User",
                CreatedAt = DateTime.Now,
                IsEmailConfirmed = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful",
                user.Id,
                user.Username,
                user.Email,
                user.Role
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

            if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username/email or password." });

            //var hashedPassword = PasswordHelper.HashPassword(request.Password);

            //var user = await _context.Users.FirstOrDefaultAsync(u =>
            //    (u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail) &&
            //    u.PasswordHash == hashedPassword);

            //if (user == null)
            //    return Unauthorized(new { message = "Invalid username/email or password." });

            if (!user.IsEmailConfirmed)
            {
                return Unauthorized(new { message = "Please confirm your email first." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

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

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Token = jwt,
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                ParkingLocationId = user.ParkingLocationId
            });
        }

        [HttpPost("send-confirmation-code")]
        [AllowAnonymous]
        public async Task<IActionResult> SendConfirmationCode([FromBody] SendConfirmationCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email is required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            var code = Random.Shared.Next(100000, 999999).ToString();

            user.EmailConfirmationCode = code;
            user.EmailConfirmationCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            Console.WriteLine($"[EMAIL VERIFY CODE] {user.Email} => {code}");

            return Ok(new
            {
                message = "Confirmation code generated successfully.",
                debugCode = code
            });
        }

        [HttpPost("verify-email-code")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailCode([FromBody] VerifyEmailCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest(new { message = "Email and code are required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.IsEmailConfirmed)
                return Ok(new { message = "Email already confirmed." });

            if (user.EmailConfirmationCode != dto.Code)
                return BadRequest(new { message = "Invalid confirmation code." });

            if (user.EmailConfirmationCodeExpiresAt == null ||
                user.EmailConfirmationCodeExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Confirmation code expired." });
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationCode = null;
            user.EmailConfirmationCodeExpiresAt = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email confirmed successfully." });
        }

        [HttpPost("resend-confirmation-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmationCode([FromBody] SendConfirmationCodeDto dto)
        {
            return await SendConfirmationCode(dto);
        }

        private bool IsStrongPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}