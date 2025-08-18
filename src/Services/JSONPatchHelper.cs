using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json.Linq;

public class JSONPatchHelper
{
    public class PatchOperation
    {
        public string? op { get; set; }
        public string? path { get; set; }
        public object? value { get; set; }
    }

    /// <summary>
    /// Applies a set of JSON Patch operations to the original object using the request payload.
    /// </summary>
    /// <typeparam name="T">Type of the original object (e.g., ShippedItemInstanceLine)</typeparam>
    /// <param name="original">The original object to patch</param>
    /// <param name="shipmentReferencePayload">The request payload (object) to patch into /shipmentReference/</param>
    /// <param name="_logger">ILogger for logging</param>
    /// <returns>The patched object of type T</returns>
    public static T PatchObject<T>(T original, JObject Payload, string parentPath, ILogger _logger) where T : class, new()
    {
        // Convert original object to JObject for path existence checks
        var originalJObject = JObject.FromObject(original);

        var patchDoc = new JsonPatchDocument();
        var patchOps = new List<PatchOperation>();
        string methodScope = "JSONPatchHelper.PatchObject: ";
        string logMessage = string.Empty;
        int indexValue = 0;

        foreach (var prop in Payload.Properties())
        {

            // Build the JSON Pointer path

            var path = $"/" + parentPath + "/{prop.Name}";

            logMessage = indexValue.ToString() + " Current path for property '" + prop.Name + "': " + path;
            if (_logger != null)
            {
                _logger.LogInformation(methodScope + logMessage);
            }

            // Check if the path exists in the original object
            // var token = originalJObject.SelectToken($"shipmentReference.{prop.Name}", false);
            var token = originalJObject.SelectToken(path, false);

            if (token != null)
            {
                // Path exists, so replace
                patchOps.Add(new PatchOperation
                {
                    op = "replace",
                    path = path,
                    value = prop.Value
                });
                patchDoc.Replace(path, prop.Value);
                _logger?.LogInformation($"Patch operation: replace {path} with value {prop.Value}");
            }
            else
            {
                // Path does not exist, so add
                patchOps.Add(new PatchOperation
                {
                    op = "add",
                    path = path,
                    value = prop.Value
                });
                patchDoc.Add(path, prop.Value);
                _logger?.LogInformation($"Patch operation: add {path} with value {prop.Value}");
            }
            
            indexValue++;
        }

        // Log the patch document
        _logger?.LogInformation("JSON Patch Document: {PatchDoc}", Newtonsoft.Json.JsonConvert.SerializeObject(patchOps));

        // Apply the patch to the original object
        var modified = JObject.FromObject(original);
        patchDoc.ApplyTo(modified);

        // Log the modified object
        _logger?.LogInformation("Modified object: {Modified}", modified.ToString());

        var result = modified.ToObject<T>();
        if (result == null)
        {
            throw new InvalidOperationException("Failed to convert patched object to type " + typeof(T).Name);
        }
        return result;
    }
}