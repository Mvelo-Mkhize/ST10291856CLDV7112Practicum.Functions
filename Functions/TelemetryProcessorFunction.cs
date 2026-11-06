using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using ST10291856CLDV7112Practicum.Functions.Models;
using System.Text.Json;

namespace ST10291856CLDV7112Practicum.Functions.Functions;

public class TelemetryProcessorFunction
{
    private readonly TableClient _table;

    public TelemetryProcessorFunction(TableServiceClient ts, IConfiguration cfg)
    {
        _table = ts.GetTableClient(cfg["TelemetryTableName"] ?? "TelemetryReadings");
        _table.CreateIfNotExists();
    }

    [Function("TelemetryProcessor")]
    public async Task Run(
        [QueueTrigger("%TelemetryQueueName%", Connection = "StorageConnection")]
        string[] messages,
        CancellationToken ct)
    {
        var entities = messages
            .Select(m => JsonSerializer.Deserialize<SoilReading>(m,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
            .Where(r => r is not null)
            .Select(r => ToEntity(r!))
            .ToList();

        if (entities.Count == 0) return;

        foreach (var group in entities.GroupBy(e => e.PartitionKey))
        {
            await _table.SubmitTransactionAsync(
                group.Select(e => new TableTransactionAction(TableTransactionActionType.UpsertMerge, e)),
                ct);
        }
    }

    private static TableEntity ToEntity(SoilReading r)
    {
        var entity = new TableEntity(
            r.SensorId,                                                       // PartitionKey
            (DateTimeOffset.MaxValue.Ticks - r.Timestamp.UtcTicks).ToString("D19")) // RowKey
        {
            ["ReadingTimestamp"] = r.Timestamp,
            ["MoisturePercent"] = r.MoisturePercent,
            ["TemperatureCelsius"] = r.TemperatureCelsius
        };
        return entity;
    }
}