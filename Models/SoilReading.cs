namespace ST10291856CLDV7112Practicum.Functions.Models;

public record SoilReading(
    string SensorId,
    double MoisturePercent,
    double TemperatureCelsius,
    DateTimeOffset Timestamp);