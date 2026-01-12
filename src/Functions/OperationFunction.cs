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

public class OperationFunction
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly string _database;

    public OperationFunction(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        _logger = loggerFactory.CreateLogger<OperationFunction>();
        _cosmosClient = cosmosClient;
        _database = System.Environment.GetEnvironmentVariable("COSMOS_DB") ?? "traceability-db";
    }

    [Function("CreateOperation")]
    public async Task<HttpResponseData> PostOperation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "traceability/V1/operation")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        OperationRequest? payload;
        try { payload = JsonConvert.DeserializeObject<IO.Swagger.Models.OperationRequest>(body); }
        catch (JsonException)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON payload.");
            return bad;
        }

        if (payload == null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid payload for Operation.");
            return bad;
        }

        var container = System.Environment.GetEnvironmentVariable("COSMOS_CONTAINER_OPERATION") ?? "operation";
        var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, container);

        try
        {
            var status = await adapter.WriteDocument(payload, _logger);
            if (status != HttpStatusCode.Created && status != HttpStatusCode.OK && status != HttpStatusCode.NoContent)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to persist operation.");
                return err;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to persist operation");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to persist operation.");
            return err;
        }

        var resp = req.CreateResponse(HttpStatusCode.Created);
        await resp.WriteStringAsync("Created");
        return resp;
    }

    [Function("PatchOperationById")]
    public async Task<HttpResponseData> PatchOperation(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "traceability/V1/operation/{id}")] HttpRequestData req,
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

        var container = System.Environment.GetEnvironmentVariable("COSMOS_CONTAINER_OPERATION") ?? "operation";
        var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, container);

        IO.Swagger.Models.OperationResponse existing = null;
        try
        {
            existing = await adapter.ReadDocument<IO.Swagger.Models.OperationResponse>(id, id, _logger);
        }
        catch (Microsoft.Azure.Cosmos.CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            existing = null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error reading operation for patch");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Error reading existing operation.");
            return err;
        }

        IO.Swagger.Models.OperationResponse toUpsert;
        if (existing == null)
        {
            toUpsert = new IO.Swagger.Models.OperationResponse();
            toUpsert = JSONPatchHelper.PatchObject(toUpsert, patchPayload, "", _logger);
        }
        else
        {
            toUpsert = JSONPatchHelper.PatchObject(existing, patchPayload, "", _logger);
        }

        try
        {
            var status = await adapter.WriteDocument<IO.Swagger.Models.OperationResponse>(toUpsert, id, _logger);
            if (status != HttpStatusCode.Created && status != HttpStatusCode.OK && status != HttpStatusCode.NoContent)
            {
                _logger.LogError("Upsert returned non-success status: {Status}", status);
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to persist patched operation.");
                return err;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert patched operation");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to persist patched operation.");
            return err;
        }

        var resp = req.CreateResponse(HttpStatusCode.OK);
        await resp.WriteStringAsync("Patched/Upserted");
        return resp;
    }
}