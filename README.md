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