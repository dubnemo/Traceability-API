
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Query.Core.Monads;
using Microsoft.AspNetCore.Routing.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TraceabilityAPI.Cosmos;
using TraceabilityAPI.Cosmos.IO;
using IO.Swagger.Models;

using static JSONPatchHelper;

namespace TraceabilityAPI;

public class TraceabilityAPI
{
    private readonly ILogger<TraceabilityAPI> _logger;
    static CosmosNoSQLAdapter? db;

    // Use this going forward:
    //

    const string databaseId = "traceability";
    const string containerId = "criticalTrackingEvents";

    // const string sqlSetupFiles = @"SELECT * FROM c where c.shipmentReference.shipFromParty.location.glnid ='{1}' AND c.shipmentReference.id = '{0}' order by c.displayName";


    // public IList<IError> faErrors { get; set; }

    public TraceabilityAPI(ILogger<TraceabilityAPI> logger)
    {
        _logger = logger;
    }

    [Function("CreateCriticalTrackingEventList")]
    public async Task<IActionResult> CreateCriticalTrackingEventList([HttpTrigger(AuthorizationLevel.Function, "post",
        Route = "traceability-api/v1/critical-tracking-event")] HttpRequest req, ILogger _logger)
    {
        IO.Swagger.Models.CriticalTrackingEventList? payload = null;

        string errorMsg = "";
        string logMessage = "";
        string methodScope = "FunctionAppTraceabilityAPI.CreateCriticalTrackingEventList - ";
        dynamic? rs = null;
        int arrayCounter = 0;

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        payload = Newtonsoft.Json.JsonConvert.DeserializeObject<IO.Swagger.Models.CriticalTrackingEventList>(requestBody);
        if (payload is null)
        {
            errorMsg = "Failed to deserialize request body to CriticalTrackingEventList.";
            _logger.LogError(methodScope + errorMsg);
            return new BadRequestObjectResult("InlogMessagevalid payload.");
        }
        logMessage = "Deserialized request body successfully.";
        _logger.LogInformation(methodScope + logMessage);



        try
        {
            foreach (var cte in payload)
            {

                db?.CreateDocument(cte, _logger);
                logMessage = "Processed cte with ID: " + cte.Id;
                _logger.LogInformation(methodScope + logMessage);
            }

            logMessage = "Processed " + arrayCounter.ToString() + " records.";
            _logger.LogInformation(methodScope + logMessage);
            
            return new JsonResult(new { status = "success", message = logMessage });
        }
        catch (CosmosException de)
        {
            Exception baseException = de.GetBaseException();
            _logger.LogInformation("CosmosDB {0} error occurred: {1}", de.StatusCode, de);
            return new JsonResult(new { error = true, errorMsg = de.Message });
        }
        catch (Exception e)
        {
            _logger.LogInformation("General error: {0}", e);
            return new JsonResult(new { error = true, errorMsg = e.Message });
        }
        finally
        {
            if (rs != null)
            {
                logMessage = "Final result: " + Newtonsoft.Json.JsonConvert.SerializeObject(rs);
                _logger.LogInformation(methodScope + logMessage);
            }
        }
    }
}