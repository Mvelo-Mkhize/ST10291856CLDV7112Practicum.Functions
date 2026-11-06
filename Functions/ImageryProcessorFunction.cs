using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ST10291856CLDV7112Practicum.Functions.Functions;

public class ImageryProcessorFunction
{
    private readonly ILogger<ImageryProcessorFunction> _log;

    public ImageryProcessorFunction(ILogger<ImageryProcessorFunction> log) => _log = log;

    [Function("ImageryProcessor")]
    public async Task Run(
        [BlobTrigger("raw-imagery/{name}",
            Source = BlobTriggerSource.EventGrid,
            Connection = "StorageConnection")]
        BlobClient blob,
        string name,
        CancellationToken ct)
    {
        var props = await blob.GetPropertiesAsync(cancellationToken: ct);
        _log.LogInformation("Imagery {Name}: {Bytes} bytes, {Type}",
            name, props.Value.ContentLength, props.Value.ContentType);
    }
}