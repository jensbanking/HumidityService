using HumidityService.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace HumidityService.FunctionHost.Functions;

/// <summary>
/// Timer-triggered function that ingests indoor climate data for every configured
/// location at the top of every hour.
/// </summary>
public sealed class IndoorClimateIngestionFunction
{
    private readonly IIndoorClimateIngestionService _ingestionService;
    private readonly ILogger<IndoorClimateIngestionFunction> _logger;

    /// <summary>Initializes a new instance of <see cref="IndoorClimateIngestionFunction"/>.</summary>
    public IndoorClimateIngestionFunction(IIndoorClimateIngestionService ingestionService, ILogger<IndoorClimateIngestionFunction> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>Runs the indoor climate ingestion pass for all configured locations.</summary>
    [Function("IndoorClimateIngestion")]
    public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Indoor climate ingestion timer fired at {TimestampUtc:o}.", DateTimeOffset.UtcNow);

        await _ingestionService.IngestAsync(cancellationToken);
    }
}
