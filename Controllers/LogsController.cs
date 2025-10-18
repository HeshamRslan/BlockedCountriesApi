using BlockedCountriesApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlockedCountriesApi.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogsController : ControllerBase
    {
        private readonly BlockedAttemptLogStore _logStore;

        public LogsController(BlockedAttemptLogStore logStore)
        {
            _logStore = logStore;
        }

        // Feature 6: Return blocked attempts log with pagination
        [HttpGet("blocked-attempts")]
        public IActionResult GetBlockedAttempts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (items, total) = _logStore.GetLogs(page, pageSize);

            return Ok(new
            {
                page,
                pageSize,
                total,
                items
            });
        }
    }
}
