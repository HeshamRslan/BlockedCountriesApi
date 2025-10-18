using System.Text.RegularExpressions;
using BlockedCountriesApi.Models;
using BlockedCountriesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlockedCountriesApi.Controllers
{
    [ApiController]
    [Route("api/countries")]
    public class CountriesController : ControllerBase
    {
        private readonly BlockedCountryStore _blockedCountryStore;
        private readonly TemporalBlockStore _temporalBlockStore;

        public CountriesController(BlockedCountryStore blockedCountryStore, TemporalBlockStore temporalBlockStore)
        {
            _blockedCountryStore = blockedCountryStore;
            _temporalBlockStore = temporalBlockStore;
        }

        // ✅ 1️⃣ Add Blocked Country (Permanent)
        [HttpPost("block")]
        public IActionResult AddBlockedCountry([FromBody] BlockedCountryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CountryCode))
                return BadRequest(new { success = false, message = "Country code is required." });

            var code = request.CountryCode.Trim().ToUpper();

            if (!Regex.IsMatch(code, @"^[A-Z]{2}$"))
                return BadRequest(new { success = false, message = "Invalid country code format. Use 2-letter ISO code (e.g., US)." });

            if (_blockedCountryStore.Exists(code))
                return Conflict(new { success = false, message = $"Country '{code}' is already permanently blocked." });

            var added = _blockedCountryStore.Add(new BlockedCountry
            {
                CountryCode = code,
                CountryName = request.CountryName,
                AddedAt = DateTime.UtcNow
            });

            if (!added)
                return StatusCode(500, new { success = false, message = "Failed to add blocked country." });

            return Ok(new { success = true, message = $"Country '{code}' has been permanently blocked." });
        }

        // ✅ 2️⃣ Remove Blocked Country (Permanent)
        [HttpDelete("block/{countryCode}")]
        public IActionResult RemoveBlockedCountry(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return BadRequest(new { success = false, message = "Country code is required." });

            var code = countryCode.Trim().ToUpper();

            if (!_blockedCountryStore.Exists(code))
                return NotFound(new { success = false, message = $"Country '{code}' is not in the blocked list." });

            var removed = _blockedCountryStore.Remove(code);

            if (!removed)
                return StatusCode(500, new { success = false, message = "Failed to remove blocked country." });

            return Ok(new { success = true, message = $"Country '{code}' has been unblocked successfully." });
        }

        // ✅ 3️⃣ Get All Blocked Countries (Permanent)
        [HttpGet("blocked")]
        public IActionResult GetBlockedCountries(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var all = _blockedCountryStore.GetAll();

            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(c =>
                    c.CountryCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.CountryName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var total = all.Count();
            var paginated = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                total,
                page,
                pageSize,
                results = paginated
            });
        }

        // ✅ 4️⃣ Temporarily Block a Country
        [HttpPost("temporal-block")]
        public IActionResult AddTemporalBlock([FromBody] TemporalBlockRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CountryCode))
                return BadRequest(new { success = false, message = "Country code is required." });

            var code = request.CountryCode.Trim().ToUpper();

            if (!Regex.IsMatch(code, @"^[A-Z]{2}$"))
                return BadRequest(new { success = false, message = "Invalid country code format. Use 2-letter ISO code (e.g., US)." });

            if (request.DurationMinutes < 1 || request.DurationMinutes > 1440)
                return BadRequest(new { success = false, message = "DurationMinutes must be between 1 and 1440 (24 hours)." });

            if (_blockedCountryStore.Exists(code))
                return Conflict(new { success = false, message = $"Country '{code}' is already permanently blocked." });

            if (_temporalBlockStore.IsBlocked(code))
                return Conflict(new { success = false, message = $"Country '{code}' is already temporarily blocked." });

            var added = _temporalBlockStore.TryAdd(code, request.DurationMinutes);
            if (!added)
                return StatusCode(500, new { success = false, message = "Failed to add temporal block." });

            return Ok(new
            {
                success = true,
                message = $"Country '{code}' temporarily blocked for {request.DurationMinutes} minutes.",
                expiresAtUtc = DateTime.UtcNow.AddMinutes(request.DurationMinutes)
            });
        }
    }
}
