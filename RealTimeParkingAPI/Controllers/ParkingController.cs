using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ParkingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("nearest")]
        public async Task<IActionResult> GetNearest(double lat, double lng)
        {
            var parks = await _context.ParkingLocations
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Latitude,
                    p.Longitude,
                    p.AllowedMinutes,
                    p.Description,
                    Distance = Math.Sqrt(Math.Pow(p.Latitude - lat, 2) + Math.Pow(p.Longitude - lng, 2))
                })
                .OrderBy(p => p.Distance)
                .Take(5)
                .ToListAsync();

            return Ok(parks);
        }
    }
}