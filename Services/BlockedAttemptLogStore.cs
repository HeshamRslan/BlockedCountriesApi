using System.Collections.Concurrent;

namespace BlockedCountriesApi.Services
{
    public class BlockAttemptLog
    {
        public string Ip { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CountryCode { get; set; } = string.Empty;
        public bool Blocked { get; set; }
        public string UserAgent { get; set; } = string.Empty;
    }

    public class BlockedAttemptLogStore
    {
        private readonly ConcurrentBag<BlockAttemptLog> _logs = new();

        public void AddAttempt(BlockAttemptLog entry)
        {
            _logs.Add(entry);
        }

        // Pagination simple (page starting from 1)
        public (IEnumerable<BlockAttemptLog> Items, int Total) GetLogs(int page = 1, int pageSize = 10)
        {
            var ordered = _logs.OrderByDescending(l => l.Timestamp).ToList();
            var total = ordered.Count;
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize);
            return (items, total);
        }
    }
}
