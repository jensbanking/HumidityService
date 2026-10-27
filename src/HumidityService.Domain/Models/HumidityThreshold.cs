namespace HumidityService.Domain.Models;

/// <summary>
/// Rules for humidity alert limits (percent relative humidity).
/// </summary>
public static class HumidityThreshold
{
    /// <summary>The limit for rooms without a configured one.</summary>
    public const double Default = 70;

    /// <summary>Whether <paramref name="percent"/> is a usable limit: greater than 0 and at most 100.</summary>
    /// <param name="percent">The limit in percent relative humidity.</param>
    public static bool IsValid(double percent) => percent is > 0 and <= 100;
}
