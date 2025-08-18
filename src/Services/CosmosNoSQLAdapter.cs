using Microsoft.Azure.Cosmos;

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

namespace TraceabilityAPI.Cosmos.IO
{


  public class CosmosNoSQLAdapter
  {
    private Action ResetConnection { get; set; }
    public DateTime ConnectionMadeDT { get; set; } = DateTime.MinValue;
    public CosmosClient client { get; private set; }
    public Container cn { get; private set; }    // private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUri"];
                                                 // private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];
                                                 // temp
                                                 // private ILogger _logger;
    private CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

    public CosmosNoSQLAdapter(CosmosClient client, string databaseId, string containerId)
    {


      this.client = client;
      cn = client.GetContainer(databaseId, containerId);

      ResetConnection = () => { return; };
      ConnectionMadeDT = DateTime.MaxValue;

    }
    public CosmosNoSQLAdapter(string connectionString, string databaseId, string containerId)
    {
      client = new CosmosClient(connectionString);
      cn = client.GetContainer(databaseId, containerId);
      ConnectionMadeDT = DateTime.UtcNow;

      ResetConnection = () =>
      {
        client = new CosmosClient(connectionString);
        cn = client.GetContainer(databaseId, containerId);
        ConnectionMadeDT = DateTime.UtcNow;
      };
    }

    public static CosmosNoSQLAdapter ConnectDB(string environConnectionParameterName, string databaseId, string containerId)
    {

      var cred = Environment.GetEnvironmentVariable(environConnectionParameterName, EnvironmentVariableTarget.Process);

      if (cred == null)
        throw new ArgumentNullException(nameof(cred), $"Environment variable '{environConnectionParameterName}' not found or is null.");

      return new CosmosNoSQLAdapter(cred, databaseId, containerId);



    }
    public CosmosNoSQLAdapter ConnectDB(string databaseId, string containerId)
    {
      return new CosmosNoSQLAdapter(client, databaseId, containerId);
    }

    public void ResetIfExpired()
    {
      var exp = DateTime.UtcNow - TimeSpan.FromHours(1);

      if (exp > ConnectionMadeDT)
        ResetConnection?.Invoke();

    }

    public void CancelCurrentOperations()
    {
      cts.Cancel();
      cts = new CancellationTokenSource();
    }

    private JObject toDocument<T>(T obj, string id)
    {
      if (obj == null)
        throw new ArgumentNullException(nameof(obj), "Object to convert to document cannot be null.");

      var jobj = JObject.FromObject(obj);
      jobj.Add(new JProperty("id", id));
      return jobj;

    }

    public async Task<HttpStatusCode> CreateDocument<T>(T data, string id, ILogger _logger)
    {
      try
      {
        var rsp = await cn.CreateItemAsync(toDocument(data, id), cancellationToken: cts.Token);
        _logger.LogInformation("CosmosNoSQLAdapter - createDocument = " + rsp.StatusCode);
        return rsp.StatusCode;
      }
      catch (CosmosException ce)
      {
        var exceptionMsg = "CosmosNoSQLAdapter - createDocument error = " + ce.StatusCode + "ceMessage = " + ce.Message;
        _logger.LogInformation("CosmosNoSQLAdapter - createDocument error = " + exceptionMsg);
        return ce.StatusCode;
      }
      catch
      {
        return HttpStatusCode.InternalServerError;
      }
    }
    public async Task<HttpStatusCode> CreateDocument<T>(T data, ILogger _logger)
    {
      _logger.LogInformation("CosmosNoSQLAdapter - createDocument - start");

      try
      {
        ItemResponse<T> rsp = await cn.CreateItemAsync(data);

        _logger.LogInformation("CosmosNoSQLAdapter - createDocument = " + rsp.StatusCode + " Message = " + rsp.Diagnostics);

        return rsp.StatusCode;
      }
      catch (CosmosException ce)
      {
        var exceptionMsg = "CosmosNoSQLAdapter - createDocument error = " + ce.StatusCode + "ceMessage = " + ce.Message;
        _logger.LogInformation("CosmosNoSQLAdapter - createDocument error = " + exceptionMsg);
        return ce.StatusCode;
      }
      catch (Exception e)
      {
        _logger.LogInformation("Exception = " + e.Message);

        return HttpStatusCode.InternalServerError;
      }
    }
    public async Task<HttpStatusCode> WriteDocument<T>(T data, string id, ILogger _logger)
    {
      try
      {
        var rsp = await cn.UpsertItemAsync(toDocument(data, id), cancellationToken: cts.Token);
        _logger.LogInformation("CosmosNoSQLAdapter.WriteDocument.WithId = " + rsp.StatusCode);
        return rsp.StatusCode;
      }
      catch (CosmosException ce)
      {
        var exceptionMsg = "CosmosNoSQLAdapter.WriteDocument.WithId error = " + ce.StatusCode + "ceMessage = " + ce.Message;
        _logger.LogInformation("CosmosNoSQLAdapter.WriteDocument.WithId error = " + exceptionMsg);
        return ce.StatusCode;
      }
      catch
      {
        return HttpStatusCode.InternalServerError;
      }
    }
    public async Task<HttpStatusCode> WriteDocument<T>(T data, ILogger _logger)
    {
      try
      {
        _logger.LogInformation("CosmosNoSQLAdapter.WriteDocument.UpsertItemAsync.Test ");
        var rsp = await cn.UpsertItemAsync(data, cancellationToken: cts.Token);
        _logger.LogInformation("CosmosNoSQLAdapter.WriteDocument = " + rsp.StatusCode);
        return rsp.StatusCode;
      }
      catch (CosmosException ce)
      {
        var exceptionMsg = "CosmosNoSQLAdapter.WriteDocument error = " + ce.StatusCode + "ceMessage = " + ce.Message;
        _logger.LogInformation("CosmosNoSQLAdapter.WriteDocument error = " + exceptionMsg);
        return ce.StatusCode;
      }
      catch (Exception e)
      {
        _logger.LogInformation("CosmosNoSQLAdapter.WriteDocument general exception = " + e.Message);
        // Log the exception message
        _logger.LogError(e, "An error occurred while writing the document.");
      
        return HttpStatusCode.InternalServerError;
      }
    }
    public async Task<T> ReadDocument<T>(string id, string partition, ILogger _logger)
    {
      var rsp = await cn.ReadItemAsync<T>(id, new PartitionKey(partition), null, cts.Token);
      return rsp.Resource;
    }
    public async Task<List<T>> QueryDocuments<T>(string query, ILogger _logger)
    {
      var list = new List<T>();
      var qDef = new QueryDefinition(query);

      _logger.LogInformation("CosmosNoSQLAdapter - QueryDocuments - query = " + query);

      var qOpt = new QueryRequestOptions { MaxConcurrency = -1, MaxBufferedItemCount = -1 };

      // add try catch to return CosmosExceptions or Exception if they occur
      //
      using var iterator = cn.GetItemQueryIterator<T>(qDef, null, qOpt);


      while (iterator.HasMoreResults)
      {
        var rsp = await iterator.ReadNextAsync(cts.Token);
        switch (rsp.StatusCode)
        {
          case HttpStatusCode.Forbidden:
            break;
          case HttpStatusCode.TooManyRequests:
            break;

          default:
            list.AddRange(rsp.Resource);
            break;
        }

        if (cts.IsCancellationRequested)
          break;
      }
      _logger.LogInformation("CosmosNoSQLAdapter - QueryDocuments - queryResponse = " + list.ToString());
      return list;
    }
    public async Task<HttpStatusCode> DeleteDocument<T>(string id, ILogger _logger)
    {
      try
      {
        var rsp = await cn.DeleteItemAsync<T>(id, new PartitionKey(id));

        return rsp.StatusCode;
      }
      catch (CosmosException ce)
      {
        var exceptionMsg = "CosmosNoSQLAdapter - DeleteDocument error = " + ce.StatusCode + "ce.Message = " + ce.Message;
        _logger.LogInformation("CosmosNoSQLAdapter - DeleteDocument error = " + exceptionMsg);
        return ce.StatusCode;
      }
      catch
      {
        return HttpStatusCode.InternalServerError;
      }

    }
  }


}
