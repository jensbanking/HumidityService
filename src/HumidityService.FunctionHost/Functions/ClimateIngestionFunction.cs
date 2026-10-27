using HumidityService.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HumidityService.FunctionHost.Functions;

/// <summary>
/// Timer-triggered function that ingests outdoor and indoor climate data for every configured
/// location, and runs the humidity alert check, at the top of every hour.
/// </summary>
public sealed class ClimateIngestionFunction
{
    private readonly IClimateIngestionService _ingestionService;
    private readonly ILogger<ClimateIngestionFunction> _logger;

    /// <summary>Initializes a new instance of <see cref="ClimateIngestionFunction"/>.</summary>
    public ClimateIngestionFunction(IClimateIngestionService ingestionService, ILogger<ClimateIngestionFunction> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>Runs the climate ingestion pass for all configured locations.</summary>
    [Function("ClimateIngestion")]
    public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Climate ingestion timer fired at {TimestampUtc:o}.", DateTimeOffset.UtcNow);

        await _ingestionService.IngestAsync(cancellationToken);
    }
}
