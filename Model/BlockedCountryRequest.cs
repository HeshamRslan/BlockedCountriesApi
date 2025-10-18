namespace BlockedCountriesApi.Models
{
    public class BlockedCountryRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string? CountryName { get; set; }
    }
}
