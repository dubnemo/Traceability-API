using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Register CosmosClient from environment; functions will construct adapter instances as needed
        var conn = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(conn))
            throw new ArgumentNullException("COSMOS_CONNECTION_STRING", "COSMOS_CONNECTION_STRING environment variable is required for Program.cs.");

        services.AddSingleton(new CosmosClient(conn));

        // register other shared services if needed
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .Build();

host.Run();
