using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IO.Swagger.Models;
using TraceabilityAPI.Cosmos.IO;
using Microsoft.Azure.Cosmos;
using System.Net;

public class ContainerFunction
{
    private readonly ILogger _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly string _database;

    public ContainerFunction(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
    {
        _logger = loggerFactory.CreateLogger<ContainerFunction>();
        _cosmosClient = cosmosClient;
        _database = System.Environment.GetEnvironmentVariable("COSMOS_DB") ?? "traceability-db";
    }

    [Function("GetContainerById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "traceability/V1/container/{id}")] HttpRequestData req,
        string id)
    {
        var containerName = System.Environment.GetEnvironmentVariable("COSMOS_CONTAINER_CONTAINER") ?? "container";
        var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, containerName);

        try
        {
            var item = await adapter.ReadDocument<IO.Swagger.Models.Container>(id, id, _logger);
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
        catch (Microsoft.Azure.Cosmos.CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Item {id} not found.");
            return notFound;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error reading container");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Error reading container.");
            return err;
        }
    }

    [Function("GetContainerList")]
    public async Task<HttpResponseData> GetList(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "traceability/V1/container-list")] HttpRequestData req)
    {
        var containerName = System.Environment.GetEnvironmentVariable("COSMOS_CONTAINER_CONTAINER") ?? "container";
        var adapter = new CosmosNoSQLAdapter(_cosmosClient, _database, containerName);

        try
        {
            var list = await adapter.QueryDocuments<IO.Swagger.Models.Container>("SELECT * FROM c", _logger);
            var resp = req.CreateResponse(HttpStatusCode.OK);
            await resp.WriteStringAsync(JsonConvert.SerializeObject(list));
            return resp;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error querying containers");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Error retrieving container list.");
            return err;
        }
    }
}