using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ST10291856CLDV7112Practicum.Functions.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ST10291856CLDV7112Practicum.Functions.Functions;

public class TelemetryIngestFunction
{
    private readonly QueueClient _queue;

    public TelemetryIngestFunction(QueueServiceClient qs, IConfiguration cfg)
    {
        _queue = qs.GetQueueClient(cfg["TelemetryQueueName"] ?? "soil-telemetry-queue");
    }

    [Function("TelemetryIngest")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "telemetry")]
        HttpRequestData req)
    {
        SoilReading? reading;
        try
        {
            reading = await JsonSerializer.DeserializeAsync<SoilReading>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON.");
            return bad;
        }

        if (reading is null || string.IsNullOrWhiteSpace(reading.SensorId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("SensorId is required.");
            return bad;
        }

        await _queue.CreateIfNotExistsAsync();
        await _queue.SendMessageAsync(Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reading))));

        var ok = req.CreateResponse(HttpStatusCode.Accepted);
        await ok.WriteAsJsonAsync(new { status = "queued", reading.SensorId });
        return ok;
    }
}