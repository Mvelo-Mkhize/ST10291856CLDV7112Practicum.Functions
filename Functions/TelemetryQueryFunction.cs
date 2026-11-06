using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace ST10291856CLDV7112Practicum.Functions.Functions;

public class TelemetryQueryFunction
{
    private readonly TableClient _table;

    public TelemetryQueryFunction(TableServiceClient ts, IConfiguration cfg)
    {
        _table = ts.GetTableClient(cfg["TelemetryTableName"] ?? "TelemetryReadings");
        _table.CreateIfNotExists();   // creates the table if it's missing
    }

    [Function("TelemetryQuery")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telemetry/{sensorId}")]
        HttpRequestData req,
        string sensorId,
        CancellationToken ct)
    {
        var results = new List<object>();

        try
        {
            await foreach (var e in _table.QueryAsync<TableEntity>(
                f => f.PartitionKey == sensorId, maxPerPage: 50, cancellationToken: ct))
            {
                results.Add(new
                {
                    PartitionKey = e.PartitionKey,
                    ReadingTimestamp = e.TryGetValue("ReadingTimestamp", out var t) ? t : null,
                    MoisturePercent = e.TryGetValue("MoisturePercent", out var m) ? m : null,
                    TemperatureCelsius = e.TryGetValue("TemperatureCelsius", out var tc) ? tc : null
                });
            }
        }
        catch (Exception ex)
        {
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync($"{ex.GetType().Name}: {ex.Message}");
            return err;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteAsJsonAsync(results);
        return ok;
    }
}