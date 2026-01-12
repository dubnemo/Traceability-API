# Traceability API
## Scope
This Function application implements the AgGateway Traceability API particularly focused on 
- Traceability Resource Units (TRU) which represents the quantity and quality measurements of an item in a container at a point in time
- Critical Tracking Events (CTE) particularly focused on the new Transfer Event where a TRU is transferred from one container into another container using a device resource, and potential having a remaining amount.  Other CTEs are historically supported including the Transport Event, Change of Custoday Event and the Change of Ownership Event.  Another new event is the Identification Event, which is the act of identification and provision of a unique identifier to record the event, item, device, container, etc.
- Key Data Elements including the shipment id, dock door id, trailer id, container id, field id, farmer id, farmer id, GLN, GTIN for product, etc.  KDEs are most often identifiers and not measurements or observations.  They help with the linkage of TRUs and CTEs.

## Endpoints
The endpoints are defined by the OpenAPI specification generated from the NIST Score tool, and developed by the AgGateway Traceability Work Group.

### Critical Tracking Event
POST /traceability/V1/critical-tracking-event
POST /traceability/V1/critical-tracking-event-list
GET /traceability/V1/critical-tracking-event-list
GET /traceability/V1/critical-tracking-event/{id}
PATCH /traceability/V1/critical-tracking-event/{id}

### Field Operations
POST /traceability/V1/operation
PATCH /traceability/V1/operation/{id}
POST /traceability/V1/operator-party
GET /traceability/V1/container/{id}
GET /traceability/V1/container-list
GET /traceability/V1/device-resource-list
GET /traceability/V1/field/{id}
GET /traceability/V1/field-list

### Traceable Resource Unit
POST /traceability/V1/traceable-resource-unit
GET /traceability/V1/traceable-resource-unit/{id}
PATCH /traceability/V1/traceable-resource-unit/{id}
GET /traceability/V1/traceable-resource-unit-list
PATCH /traceability/traceable-resource-unit/{id}/V1/container-state

# Co-Pilot Prompt
Complete this function app by adding an HttpTrigger implementation of all the endpoints specified in the OpenAPI yml file in this project, ensuring that parameters are properly handled as specified with the endpoint.  Each JSON payload in either the request body or response will reference the appropriate 'type' specified in the Model folder as related to the resource definition in the endpoint path, with a conversion from the '-' dash notation to the UpperCamelCase notation defined in the Model. All JSON will be persisted in the Cosmos DB.  All PATCH operations will implement the upsert pattern, by retrieving the JSON by id or uuid, applying the Newtonsoft JSONPatch method already created in the helper class in this project, then replacing the JSON in the Cosmos DB container.

Sure, but use the existing workspace, the existing CosmosNoSQLAdapter.cs capabilities rewriting if necessary, and retain the OpenAPI yml file in the existing yml folder.  Ensure the isolidate worker model is used, as in the existing function-app-traceability-api.cs files.   Remove the function-app-traceability-api.cs file after migrating to CriticalTrackingEventListFunction.cs


Define the next steps to add new Agentic capabilities to the traceability capabilities defined in this OpenAPI yml.  Determine if the exist function app can be extended (ideal) or whether it is necessary to create a new agent app.  Ensure the the necessary MCP server protocols are able to interoperate with the OpenAPI specification in this yml. Define a mapping from the OpenAPI yml description keywords to that needed in the agent registry.