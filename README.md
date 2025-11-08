# Traceability API
## Scope
This Function application implements the AgGateway Traceability API particularly focused on 
- Traceability Resource Units (TRU) which represents the quantity and quality measurements of an item in a container at a point in time
- Critical Tracking Events (CTE) particularly focused on the new Transfer Event where a TRU is transferred from one container into another container using a device resource, and potential having a remaining amount.  Other CTEs are historically supported including the Transport Event, Change of Custoday Event and the Change of Ownership Event.  Another new event is the Identification Event, which is the act of identification and provision of a unique identifier to record the event, item, device, container, etc.
- Key Data Elements including the shipment id, dock door id, trailer id, container id, field id, farmer id, farmer id, GLN, GTIN for product, etc.  KDEs are most often identifiers and not measurements or observations.  They help with the linkage of TRUs and CTEs.

## Endpoints
The endpoints are defined by the OpenAPI specification generated from the NIST Score tool, and developed by the AgGateway Traceability Work Group.

