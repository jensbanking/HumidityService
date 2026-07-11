using HumidityService.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HumidityService.FunctionHost.Functions;

/// <summary>
/// Timer-triggered function that ingests outdoor climate data for every configured
/// location at the top of every hour.
/// </summary>
public sealed class OutdoorClimateIngestionFunction
{
    private readonly IOutdoorClimateIngestionService _ingestionService;
    private readonly ILogger<OutdoorClimateIngestionFunction> _logger;

    /// <summary>Initializes a new instance of <see cref="OutdoorClimateIngestionFunction"/>.</summary>
    public OutdoorClimateIngestionFunction(IOutdoorClimateIngestionService ingestionService, ILogger<OutdoorClimateIngestionFunction> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>Runs the outdoor climate ingestion pass for all configured locations.</summary>
    [Function("OutdoorClimateIngestion")]
    public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outdoor climate ingestion timer fired at {TimestampUtc:o}.", DateTimeOffset.UtcNow);

        await _ingestionService.IngestAsync(cancellationToken);
    }
}
