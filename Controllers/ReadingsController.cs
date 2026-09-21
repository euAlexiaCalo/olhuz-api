using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using olhuz.API.Models.DTOs.Readings;
using olhuz.API.Services;
using System.Security.Claims;
using olhuz.API.Services.Interfaces;

namespace olhuz.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReadingsController : ControllerBase
    {
        private readonly IReadingService _readingService;

        public ReadingsController(IReadingService readingService)
        {
            _readingService = readingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserReadings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();

            Guid userId = Guid.Parse(userIdClaim);
            var result = await _readingService.GetReadingsByUserIdAsync(userId);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReading([FromForm] CreateReadingDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();

            Guid userId = Guid.Parse(userIdClaim);
            var result = await _readingService.CreateReadingAsync(userId, dto);

            if (result.Error)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}