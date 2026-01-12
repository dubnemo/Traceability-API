using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IO.Swagger.Models;
using TraceabilityAPI.Cosmos.IO;
using Microsoft.Azure.Cosmos;
using System.Net;


public class CriticalTrackingEventFunction
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly string _database;

    // updated constructor: accept CosmosClient (registered in DI) and construct adapter per request
    public CriticalTrackingEventFunction(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        _logger = loggerFactory.CreateLogger<CriticalTrackingEventFunction>();
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _database = Environment.GetEnvironmentVariable("COSMOS_DB") ?? "traceability-db";
    }

    [Function("CreateCriticalTrackingEvent")]
    public async Task<HttpResponseData> PostCriticalTrackingEvent(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "traceability/V1/critical-tracking-event")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        CriticalTrackingEventList? payload;
        try
        {
            payload = JsonConvert.DeserializeObject<CriticalTrackingEventList>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Payload deserialization failed");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON payload.");
            return bad;
        }

        if (payload is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid payload for CriticalTrackingEventList.");
            return bad;
        }

        var containerName = Environment.GetEnvironmentVariable("COSMOS_CONTAINER_CTE") ?? "critical-tracking-event";

        try
        {
            var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, containerName);
            var status = await adapter.WriteDocument<CriticalTrackingEventList>(payload, _logger);

            if (status != HttpStatusCode.Created && status != HttpStatusCode.OK && status != HttpStatusCode.NoContent)
            {
                _logger.LogError("Cosmos write returned non-success status: {Status}", status);
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to persist payload.");
                return err;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to persist critical tracking event");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to persist payload.");
            return err;
        }

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteStringAsync("Created");
        return response;
    }

    [Function("CreateCriticalTrackingEventList")]
    public async Task<HttpResponseData> PostCriticalTrackingEventList(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "traceability/V1/critical-tracking-event-list")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        CriticalTrackingEventList? payload;
        try
        {
            payload = JsonConvert.DeserializeObject<CriticalTrackingEventList>(body);
        }
        catch (JsonException)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON payload.");
            return bad;
        }

        if (payload == null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid payload for CriticalTrackingEventList.");
            return bad;
        }

        var containerName = Environment.GetEnvironmentVariable("COSMOS_CONTAINER_CTE_LIST") ?? "critical-tracking-event-list";
        try
        {
            var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, containerName);
            var status = await adapter.WriteDocument<CriticalTrackingEventList>(payload, _logger);

            if (status != HttpStatusCode.Created && status != HttpStatusCode.OK && status != HttpStatusCode.NoContent)
            {
                _logger.LogError("Cosmos write returned non-success status: {Status}", status);
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to persist payload.");
                return err;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to persist critical tracking event list");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to persist payload.");
            return err;
        }

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteStringAsync("Created");
        return response;
    }
}