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

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // km
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRad(double value)
        {
            return value * Math.PI / 180;
        }

        [HttpGet("nearest")]
        public async Task<IActionResult> GetNearest(double lat, double lng)
        {
            var parkings = await _context.ParkingLocations.ToListAsync();

            var result = parkings
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Latitude,
                    p.Longitude,
                    p.AllowedMinutes,
                    p.Description,
                    Distance = GetDistance(lat, lng, p.Latitude, p.Longitude)
                })
                .Where(p => p.Distance <= 2) // 🔥 STRICT RADIUS (2 km)
                .OrderBy(p => p.Distance)
                .Take(10);

            return Ok(result);
        }
    }
}