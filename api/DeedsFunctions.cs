using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class DeedsFunctions
{
    private readonly string _cs;

    public DeedsFunctions(DbOptions options)
    {
        _cs = options.ConnectionString;
    }

    [Function("CreateDeed")]
    public async Task<HttpResponseData> CreateDeed(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "deeds")] HttpRequestData req)
    {
        CreateDeed? payload;
        try
        {
            payload = await req.ReadFromJsonAsync<CreateDeed>();
        }
        catch (JsonException)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON payload");
        }

        if (payload is null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body required");
        }

        if (payload.ChildId == Guid.Empty || payload.DeedTypeId == Guid.Empty || payload.CreatedBy == Guid.Empty)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "ChildId, DeedTypeId and CreatedBy are required");
        }

        var child = await Data.GetChildById(_cs, payload.ChildId);
        if (child is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        if (child.ParentId != payload.CreatedBy)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Forbidden, "You cannot log deeds for another parent");
        }

        var deedType = await Data.GetDeedTypeById(_cs, payload.DeedTypeId);
        if (deedType is null || deedType.ParentId != child.ParentId)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Deed type not found for this parent");
        }

        if (!deedType.Active)
        {
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, "Deed type is inactive");
        }

        var points = payload.Points != 0 ? payload.Points : deedType.Points;
        if (points == 0)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Points must resolve to a non-zero value");
        }

        var note = string.IsNullOrWhiteSpace(payload.Note) ? null : payload.Note.Trim();
        var occurredAt = DateTimeOffset.UtcNow;

        var created = await Data.CreateDeed(_cs, payload.ChildId, payload.DeedTypeId, points, note, payload.CreatedBy, occurredAt);
        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(created);
        return res;
    }

    [Function("ListDeedsForChild")]
    public async Task<HttpResponseData> ListDeedsForChild(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "children/{childId:guid}/deeds")] HttpRequestData req,
        Guid childId)
    {
        var child = await Data.GetChildById(_cs, childId);
        if (child is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var deeds = await Data.GetDeedsForChild(_cs, childId);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(deeds);
        return res;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
    {
        var res = req.CreateResponse(status);
        await res.WriteAsJsonAsync(new { error = message });
        return res;
    }
}
