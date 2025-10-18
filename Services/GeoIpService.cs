using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlockedCountriesApi.Services
{
    public class GeoIpService : IGeoIpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public GeoIpService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<GeoIpResult> LookupAsync(string ip)
        {
            var apiKey = _configuration["GeoIp:ApiKey"] ?? string.Empty;
            var baseUrl = _configuration["GeoIp:BaseUrl"] ?? string.Empty;

            var client = _httpClientFactory.CreateClient();

            string url;
            if (!string.IsNullOrWhiteSpace(apiKey) && baseUrl.Contains("ipgeolocation"))
                url = $"{baseUrl}?apiKey={apiKey}&ip={ip}";
            else if (baseUrl.Contains("ipapi.co"))
                url = $"{baseUrl}/{ip}/json/";
            else
                url = $"{baseUrl}?apiKey={apiKey}&ip={ip}"; // fallback

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            // extract fields from common providers
            var countryCode = (string?)(
                data["country_code2"] ??
                data["country"] ??
                data["country_code"] ??
                data["countryCode"] ?? "N/A"
            ) ?? "N/A";

            var countryName = (string?)(
                data["country_name"] ??
                data["country_name_en"] ??
                data["country_name"] ?? "Unknown"
            ) ?? "Unknown";

            var isp = (string?)(
                data["isp"] ??
                data["org"] ??
                data["organization"] ??
                data.SelectToken("connection.isp") ??
                data.SelectToken("company.name")
            ) ?? "Unknown";

            return new GeoIpResult(ip, countryCode.ToUpper(), countryName, isp, json);
        }
    }
}
