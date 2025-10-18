namespace BlockedCountriesApi.Models
{
    public class TemporalBlock
    {
        public string CountryCode { get; set; } = string.Empty; // "US"
        public DateTime ExpiryUtc { get; set; } // وقت الانتهاء بالتوقيت العالمي
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
