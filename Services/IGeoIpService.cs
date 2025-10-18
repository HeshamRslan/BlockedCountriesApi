using System.Threading.Tasks;

namespace BlockedCountriesApi.Services
{
    public record GeoIpResult(string Ip, string CountryCode, string CountryName, string Isp, string RawJson);

    public interface IGeoIpService
    {
        Task<GeoIpResult> LookupAsync(string ip);
    }
}
