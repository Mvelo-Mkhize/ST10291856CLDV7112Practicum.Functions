using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((ctx, services) =>
    {
        var conn = ctx.Configuration["StorageConnection"]!;
        services.AddSingleton(_ => new BlobServiceClient(conn));
        services.AddSingleton(_ => new QueueServiceClient(conn));
        services.AddSingleton(_ => new TableServiceClient(conn));
    })
    .Build();

host.Run();