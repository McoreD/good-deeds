using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class DeedTypesFunctions
{
    private readonly string _cs;

    public DeedTypesFunctions(DbOptions options)
    {
        _cs = options.ConnectionString;
    }

    [Function("CreateDeedType")]
    public async Task<HttpResponseData> CreateDeedType(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "parents/{parentId:guid}/deed-types")] HttpRequestData req,
        Guid parentId)
    {
        CreateDeedType? payload;
        try
        {
            payload = await req.ReadFromJsonAsync<CreateDeedType>();
        }
        catch (JsonException)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON payload");
        }

        if (payload is null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body required");
        }

        if (payload.ParentId != Guid.Empty && payload.ParentId != parentId)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "ParentId mismatch");
        }

        var parent = await Data.GetParentById(_cs, parentId);
        if (parent is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Name is required");
        }

        if (payload.Points == 0)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Points must be non-zero to indicate good or bad deed");
        }

        var normalizedName = payload.Name.Trim();
        var existing = await Data.GetDeedTypeByName(_cs, parentId, normalizedName);
        if (existing is not null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, "Deed type already exists");
        }

        var created = await Data.CreateDeedType(_cs, parentId, normalizedName, payload.Points);
        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(created);
        return res;
    }

    [Function("ListDeedTypes")]
    public async Task<HttpResponseData> ListDeedTypes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "parents/{parentId:guid}/deed-types")] HttpRequestData req,
        Guid parentId)
    {
        var parent = await Data.GetParentById(_cs, parentId);
        if (parent is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var items = await Data.GetDeedTypesForParent(_cs, parentId);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(items);
        return res;
    }

    [Function("UpdateDeedType")]
    public async Task<HttpResponseData> UpdateDeedType(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", "patch", Route = "deed-types/{deedTypeId:guid}")] HttpRequestData req,
        Guid deedTypeId)
    {
        UpdateDeedType? payload;
        try
        {
            payload = await req.ReadFromJsonAsync<UpdateDeedType>();
        }
        catch (JsonException)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON payload");
        }

        if (payload is null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body required");
        }

        var existing = await Data.GetDeedTypeById(_cs, deedTypeId);
        if (existing is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        if (payload.ParentId != Guid.Empty && payload.ParentId != existing.ParentId)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, "ParentId mismatch");
        }

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Name is required");
        }

        if (payload.Points == 0)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Points must be non-zero");
        }

        var normalizedName = payload.Name.Trim();
        var conflicting = await Data.GetDeedTypeByName(_cs, existing.ParentId, normalizedName);
        if (conflicting is not null && conflicting.Id != deedTypeId)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, "Another deed type with that name exists");
        }

        var updated = await Data.UpdateDeedType(_cs, deedTypeId, normalizedName, payload.Points, payload.Active);
        if (updated is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(updated);
        return res;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
    {
        var res = req.CreateResponse(status);
        await res.WriteAsJsonAsync(new { error = message });
        return res;
    }
}
