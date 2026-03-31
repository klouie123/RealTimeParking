using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeParkingAPI.Data;

namespace RealTimeParkingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingLocationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParkingLocationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var locations = await _context.ParkingLocations
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(locations);
        }
    }
}
