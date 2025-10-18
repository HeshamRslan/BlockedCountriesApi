using System.Collections.Concurrent;
using BlockedCountriesApi.Models;

namespace BlockedCountriesApi.Services
{
    public class TemporalBlockStore
    {
        private readonly ConcurrentDictionary<string, TemporalBlock> _store = new();

        // Add — returns false if already exists
        public bool TryAdd(string countryCode, int durationMinutes)
        {
            var key = countryCode.ToUpper();
            var expiry = DateTime.UtcNow.AddMinutes(durationMinutes);
            var block = new TemporalBlock
            {
                CountryCode = key,
                ExpiryUtc = expiry,
                CreatedAtUtc = DateTime.UtcNow
            };

            return _store.TryAdd(key, block);
        }

        // Check if currently temporally blocked (and not expired)
        public bool IsBlocked(string countryCode)
        {
            var key = countryCode.ToUpper();
            if (_store.TryGetValue(key, out var block))
            {
                if (block.ExpiryUtc <= DateTime.UtcNow)
                {
                    // expired -> remove and return false
                    _store.TryRemove(key, out _);
                    return false;
                }
                return true;
            }
            return false;
        }

        // Remove (manual or cleanup)
        public bool Remove(string countryCode)
            => _store.TryRemove(countryCode.ToUpper(), out _);

        // Cleanup expired entries
        public void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            var keys = _store.Keys.ToList();
            foreach (var key in keys)
            {
                if (_store.TryGetValue(key, out var block) && block.ExpiryUtc <= now)
                    _store.TryRemove(key, out _);
            }
        }

        // For inspection / admin
        public IEnumerable<TemporalBlock> GetAll() => _store.Values.OrderByDescending(b => b.CreatedAtUtc);
    }
}
