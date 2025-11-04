
using System.Net;

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

    static CosmosNoSQLAdapter? db;

    // Use this going forward:
    //

    const string databaseId = "traceability";
    const string containerId = "criticalTrackingEvents";

    // const string sqlSetupFiles = @"SELECT * FROM c where c.shipmentReference.shipFromParty.location.glnid ='{1}' AND c.shipmentReference.id = '{0}' order by c.displayName";


    [Function("CreateCriticalTrackingEventList")]
    public async Task<HttpResponseData> CreateCriticalTrackingEventList([HttpTrigger(AuthorizationLevel.Function, "post",
        Route = "traceability-api/v1/critical-tracking-event")] HttpRequestData req, FunctionContext context)
    {


        IO.Swagger.Models.CriticalTrackingEventList? payload = null;

        string errorMsg = "";
        string logMessage = " - received request";
        var _logger = context.GetLogger(context.FunctionDefinition.Name);
        _logger.LogInformation($"[{context.FunctionDefinition.Name}] + {logMessage}");

        dynamic? rs = null;

        HttpResponseData response;

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        payload = Newtonsoft.Json.JsonConvert.DeserializeObject<IO.Swagger.Models.CriticalTrackingEventList>(requestBody);
        if (payload is null)
        {
            errorMsg = "Failed to deserialize request body to CriticalTrackingEventList.";
            response = req.CreateResponse(HttpStatusCode.PreconditionRequired); // 428 Precondition Required
            await response.WriteAsJsonAsync(new { function = context.FunctionDefinition.Name, error = true, errorMsg = errorMsg });
            return response;
        }
        logMessage = " = Deserialized request body successfully.";
        _logger.LogInformation($"[{context.FunctionDefinition.Name}] + {logMessage}");


        try
        {
            foreach (var cte in payload)
            {

                db?.CreateDocument(cte, _logger);
                logMessage = "Processed cte with ID: " + cte.Id;
                _logger.LogInformation( logMessage);
            }

            _logger.LogInformation($"[{context.FunctionDefinition.Name}] + {logMessage}");

            response = req.CreateResponse(HttpStatusCode.Created); // 201 Created
            await response.WriteAsJsonAsync(new { function = context.FunctionDefinition.Name, status = "success", message = logMessage });
            return response;

        }
        catch (CosmosException de)
        {
            Exception baseException = de.GetBaseException();
            _logger.LogError($"[{context.FunctionDefinition.Name}] {errorMsg}");

            response = req.CreateResponse(HttpStatusCode.InternalServerError); // 501 Precondition Required
            await response.WriteAsJsonAsync(new { function = context.FunctionDefinition.Name, error = true, errorMsg = errorMsg });
            return response;
        }
        catch (Exception e)
        {
            response = req.CreateResponse(HttpStatusCode.InternalServerError); // 501 Precondition Required
            await response.WriteAsJsonAsync(new { function = context.FunctionDefinition.Name, error = true, errorMsg = errorMsg, errorDetail = e.Message });
            return response;
        }
        finally
        {
            if (rs != null)
            {
                logMessage = "Final result: " + Newtonsoft.Json.JsonConvert.SerializeObject(rs);
                _logger.LogInformation( logMessage);
            }
        }
    }
}