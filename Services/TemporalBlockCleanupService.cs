using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BlockedCountriesApi.Services
{
    public class TemporalBlockCleanupService : BackgroundService
    {
        private readonly TemporalBlockStore _temporalBlockStore;
        private readonly ILogger<TemporalBlockCleanupService> _logger;

        public TemporalBlockCleanupService(TemporalBlockStore temporalBlockStore, ILogger<TemporalBlockCleanupService> logger)
        {
            _temporalBlockStore = temporalBlockStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TemporalBlockCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _temporalBlockStore.CleanupExpired();
                    _logger.LogInformation("Temporal cleanup executed at {time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during temporal cleanup.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogInformation("TemporalBlockCleanupService stopping.");
        }
    }
}
