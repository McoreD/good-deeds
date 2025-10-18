using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class Api
{
    private readonly string _cs;
    public Api(DbOptions options)
    {
        _cs = options.ConnectionString;
    }

    [Function("Health")]
    public async Task<HttpResponseData> Health([HttpTrigger(AuthorizationLevel.Anonymous,"get",Route="health")] HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteStringAsync("ok");
        return res;
    }
}
