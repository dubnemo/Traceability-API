using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IO.Swagger.Models;
using TraceabilityAPI.Cosmos.IO;
using Microsoft.Azure.Cosmos;
using System.Net;

public class OperatorPartyFunction
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly string _database;

    public OperatorPartyFunction(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        _logger = loggerFactory.CreateLogger<OperatorPartyFunction>();
        _cosmosClient = cosmosClient;
        _database = System.Environment.GetEnvironmentVariable("COSMOS_DB") ?? "traceability-db";
    }

    [Function("CreateOperatorParty")]
    public async Task<HttpResponseData> PostOperatorParty(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "traceability/V1/operator-party")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        OperatorParty? payload;
        try { payload = JsonConvert.DeserializeObject<OperatorParty>(body); }
        catch (JsonException)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid JSON payload.");
            return bad;
        }

        if (payload == null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid payload for OperatorParty.");
            return bad;
        }

        var container = System.Environment.GetEnvironmentVariable("COSMOS_CONTAINER_OPERATOR") ?? "operator-party";
        var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, container);

        try
        {
            var status = await adapter.WriteDocument(payload, _logger);
            if (status != HttpStatusCode.Created && status != HttpStatusCode.OK && status != HttpStatusCode.NoContent)
            {
                var err = req.CreateResponse(HttpStatusCode.InternalServerError);
                await err.WriteStringAsync("Failed to persist operator party.");
                return err;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to persist operator party");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to persist operator party.");
            return err;
        }

        var resp = req.CreateResponse(HttpStatusCode.Created);
        await resp.WriteStringAsync("Created");
        return resp;
    }
}