using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IO.Swagger.Models;
using TraceabilityAPI.Cosmos.IO;
using Microsoft.Azure.Cosmos;
using System.Net;

public class CriticalTrackingEventListFunction
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly string _database;

    public CriticalTrackingEventListFunction(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        _logger = loggerFactory.CreateLogger<CriticalTrackingEventListFunction>();
        _cosmosClient = cosmosClient ?? throw new System.ArgumentNullException(nameof(cosmosClient));
        _database = System.Environment.GetEnvironmentVariable("COSMOS_DB") ?? "traceability-db";
    }

    [Function("GetCriticalTrackingEventList")]
    public async Task<HttpResponseData> GetList(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "traceability/V1/critical-tracking-event-list")] HttpRequestData req)
    {
        var container = System.Environment.GetEnvironmentVariable("COSMOS_CONTAINER_CTE_LIST") ?? "critical-tracking-event-list";
        var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, container);

        try
        {
            var list = await adapter.QueryDocuments<CriticalTrackingEventList>("SELECT * FROM c", _logger);
            var resp = req.CreateResponse(HttpStatusCode.OK);
            await resp.WriteStringAsync(JsonConvert.SerializeObject(list));
            return resp;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error querying critical tracking event list");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Error retrieving list.");
            return err;
        }
    }
}