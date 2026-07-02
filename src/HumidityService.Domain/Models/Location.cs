namespace HumidityService.Domain.Models;

/// <summary>
/// A single statically-configured location (e.g. a house) that indoor and outdoor
/// climate readings are collected for. The same <see cref="Slug"/> is used for both
/// indoor and outdoor blob naming so readings for a location can be correlated.
/// </summary>
/// <param name="Slug">URL/path-safe identifier (lowercase, no spaces), e.g. "aarhus-house1".</param>
/// <param name="Latitude">Latitude used for the Open-Meteo outdoor query.</param>
/// <param name="Longitude">Longitude used for the Open-Meteo outdoor query.</param>
public sealed record Location(string Slug, double Latitude, double Longitude);
