using System.Collections.Concurrent;

namespace BlockedCountriesApi.Services
{
    public class BlockedCountry
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class BlockedCountryStore
    {
        private readonly ConcurrentDictionary<string, BlockedCountry> _blockedCountries = new();

        public IEnumerable<BlockedCountry> GetAll() => _blockedCountries.Values;

        public bool Add(BlockedCountry country)
            => _blockedCountries.TryAdd(country.CountryCode.ToUpper(), country);

        public bool Exists(string countryCode)
            => _blockedCountries.ContainsKey(countryCode.ToUpper());

        public bool Remove(string countryCode)
            => _blockedCountries.TryRemove(countryCode.ToUpper(), out _);
    }
}
