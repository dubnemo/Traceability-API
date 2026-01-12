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


public class CriticalTrackingEventByIdFunction
{
    private readonly ILogger _logger;
    private readonly CosmosNoSQLAdapter _cosmos;

    public CriticalTrackingEventByIdFunction(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        _logger = loggerFactory.CreateLogger<CriticalTrackingEventByIdFunction>();

        // Use environment settings for DB/container; default container name for CTEs
        var database = Environment.GetEnvironmentVariable("COSMOS_DB") ?? "traceability-db";
        var container = Environment.GetEnvironmentVariable("COSMOS_CONTAINER_CTE") ?? "critical-tracking-event";

        // Create adapter instance using your exact adapter constructor
        _cosmos = new CosmosNoSQLAdapter(cosmosClient, database, container);
    }

    [Function("GetCriticalTrackingEventById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "traceability/V1/critical-tracking-event/{id}")] HttpRequestData req,
        string id)
    {
        CriticalTrackingEventList item = null;
        try
        {
            item = await _cosmos.ReadDocument<CriticalTrackingEventList>(id, id, _logger);
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            item = null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error reading document");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Error reading document.");
            return err;
        }

        if (item == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Item {id} not found.");
            return notFound;
        }

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync(JsonConvert.SerializeObject(item));
        return ok;
    }

    [Function("PatchCriticalTrackingEventById")]
    public async Task<HttpResponseData> PatchById(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "traceability/V1/critical-tracking-event/{id}")] HttpRequestData req,
        string id)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        JObject patchPayload;
        try
        {
            patchPayload = JObject.Parse(body);
        }
        catch (JsonException)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON payload.");
            return bad;
        }

        CriticalTrackingEventList existing = null;
        try
        {
            existing = await _cosmos.ReadDocument<CriticalTrackingEventList>(id, id, _logger);
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            existing = null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error reading document for patch");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Error reading existing document.");
            return err;
        }

        CriticalTrackingEventList toUpsert;
        if (existing == null)
        {
            toUpsert = new CriticalTrackingEventList();
            toUpsert = JSONPatchHelper.PatchObject(toUpsert, patchPayload, "shipmentReference", _logger);
        }
        else
        {
            toUpsert = JSONPatchHelper.PatchObject(existing, patchPayload, "shipmentReference", _logger);
        }

        try
        {
            var status = await _cosmos.WriteDocument<CriticalTrackingEventList>(toUpsert, id, _logger);
            if (status != HttpStatusCode.Created && status != HttpStatusCode.OK && status != HttpStatusCode.NoContent)
            {
                _logger.LogError("Upsert returned non-success status: {Status}", status);
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to persist patched object.");
                return err;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert patched object");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to persist patched object.");
            return err;
        }

        var resp = req.CreateResponse(HttpStatusCode.OK);
        await resp.WriteStringAsync("Patched/Upserted");
        return resp;
    }
}