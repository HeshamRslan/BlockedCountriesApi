using System.Net;
using System.Net.Http;
using BlockedCountriesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace BlockedCountriesApi.Controllers
{
    [ApiController]
    [Route("api/ip")]
    public class IpController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly IGeoIpService _geoService;
        private readonly BlockedCountryStore _blockedStore;
        private readonly TemporalBlockStore _temporalBlockStore;
        private readonly BlockedAttemptLogStore _logStore;

        public IpController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IGeoIpService geoService,
            BlockedCountryStore blockedStore,
            BlockedAttemptLogStore logStore,
            TemporalBlockStore temporalBlockStore)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _geoService = geoService;
            _blockedStore = blockedStore;
            _logStore = logStore;
            _temporalBlockStore = temporalBlockStore;
        }

        // Lookup IP Geo Info
        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? ipAddress)
        {
            // 1️ Determine IP
            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(ipAddress))
                return BadRequest(new { success = false, message = "Unable to determine client IP address." });

            // Handle localhost
            if (ipAddress == "127.0.0.1" || ipAddress == "::1")
                return Ok(new { success = true, data = new { ip = ipAddress, countryCode = "LOCAL", countryName = "Localhost", isp = "Local Network" } });

            // 2️ Validate IP format
            if (!IPAddress.TryParse(ipAddress, out _))
                return BadRequest(new { success = false, message = "Invalid IP address format." });

            // 3️⃣ Check Cache
            if (_cache.TryGetValue(ipAddress, out var cached))
                return Ok(new { success = true, data = cached });

            // 4️ Load API config
            var apiKey = _configuration["GeoIp:ApiKey"];
            var baseUrl = _configuration["GeoIp:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
                return StatusCode(500, new { success = false, message = "GeoIP configuration is missing." });

            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl}?apiKey={apiKey}&ip={ipAddress}";

            try
            {
                Console.WriteLine($"🔍 Calling Geo API: {url}");
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to fetch data from Geo API." });

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                var countryCode = data["country_code2"]?.ToString() ?? "N/A";
                var countryName = data["country_name"]?.ToString() ?? "Unknown";
                string? isp = data.SelectToken("org")?.Value<string>()
                                 ?? data.SelectToken("isp")?.Value<string>()
                                 ?? data.SelectToken("asn_org")?.Value<string>()
                                 ?? data.SelectToken("connection.isp")?.Value<string>()
                                 ?? data.SelectToken("company.name")?.Value<string>();

                if (isp == null && data.SelectToken("org") is JArray arr && arr.Count > 0)
                    isp = arr.First?.ToString();

                if (string.IsNullOrWhiteSpace(isp))
                    isp = "Unknown";

                var result = new { ip = ipAddress, countryCode, countryName, isp };

                _cache.Set(ipAddress, result, TimeSpan.FromHours(6)); // cache for 6 hours

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching IP info.", error = ex.Message });
            }
        }

        // Check if IP Country is Blocked
        [HttpGet("check-block")]
        public async Task<IActionResult> CheckBlock([FromQuery] string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(ipAddress))
                return BadRequest(new { success = false, message = "Unable to determine client IP address." });

            if (!IPAddress.TryParse(ipAddress, out _))
                return BadRequest(new { success = false, message = "Invalid IP address format." });

            GeoIpResult geo;
            try
            {
                geo = await _geoService.LookupAsync(ipAddress);
            }
            catch (HttpRequestException)
            {
                try
                {
                    _logStore.AddAttempt(new BlockAttemptLog
                    {
                        Ip = ipAddress,
                        Timestamp = DateTime.UtcNow,
                        CountryCode = "N/A",
                        Blocked = false,
                        UserAgent = Request.Headers["User-Agent"].ToString()
                    });
                }
                catch { }

                return StatusCode(503, new { success = false, message = "Failed to fetch Geo info from external provider." });
            }

            var permanentlyBlocked = _blockedStore.Exists(geo.CountryCode);
            var temporallyBlocked = _temporalBlockStore.IsBlocked(geo.CountryCode);
            var isBlocked = permanentlyBlocked || temporallyBlocked;

            try
            {
                _logStore.AddAttempt(new BlockAttemptLog
                {
                    Ip = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    CountryCode = geo.CountryCode,
                    Blocked = isBlocked,
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"⚠️ Log failed: {logEx.Message}");
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    ip = ipAddress,
                    geo.CountryCode,
                    geo.CountryName,
                    geo.Isp,
                    blocked = isBlocked
                }
            });
        }
    }
}
