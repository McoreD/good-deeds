using System.Net.Http.Json;
using GoodDeeds.Client.Models;

namespace GoodDeeds.Client.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ParentDto> CreateParentAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("parents", new CreateParentRequest(email));
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<ParentDto>())!;
        }

        var error = await ReadErrorAsync(response);
        throw new InvalidOperationException(error ?? "Unable to create parent");
    }

    public async Task<ParentDto?> FindParentByEmailAsync(string email)
    {
        var response = await _http.GetAsync($"parents?email={Uri.EscapeDataString(email)}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ParentDto>();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        var error = await ReadErrorAsync(response);
        throw new InvalidOperationException(error ?? "Unable to lookup parent");
    }

    public async Task<ParentDto?> GetParentAsync(Guid parentId)
    {
        return await _http.GetFromJsonAsync<ParentDto>($"parents/{parentId}");
    }

    public async Task<IReadOnlyList<ChildDto>> GetChildrenAsync(Guid parentId)
    {
    var items = await _http.GetFromJsonAsync<List<ChildDto>>($"parents/{parentId}/children");
    return items ?? new List<ChildDto>();
    }

    public async Task<ChildDto> CreateChildAsync(Guid parentId, string name, decimal dollarPerPoint)
    {
        var request = new CreateChildRequest(parentId, name, dollarPerPoint);
        var response = await _http.PostAsJsonAsync($"parents/{parentId}/children", request);
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<ChildDto>())!;
        }

        var error = await ReadErrorAsync(response);
        throw new InvalidOperationException(error ?? "Unable to create child");
    }

    public async Task<ChildDto?> GetChildAsync(Guid childId)
    {
        return await _http.GetFromJsonAsync<ChildDto>($"children/{childId}");
    }

    public async Task<ChildDto?> UpdateChildAsync(Guid childId, Guid parentId, string name, decimal dollarPerPoint)
    {
        var request = new UpdateChildRequest(parentId, name, dollarPerPoint);
        var response = await _http.PutAsJsonAsync($"children/{childId}", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ChildDto>();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        var error = await ReadErrorAsync(response);
        throw new InvalidOperationException(error ?? "Unable to update child");
    }

    public async Task DeleteChildAsync(Guid parentId, Guid childId)
    {
        var response = await _http.DeleteAsync($"parents/{parentId}/children/{childId}");
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return;
            }

            var error = await ReadErrorAsync(response);
            throw new InvalidOperationException(error ?? "Unable to delete child");
        }
    }

    public async Task<BalanceDto?> GetChildBalanceAsync(Guid childId)
    {
        return await _http.GetFromJsonAsync<BalanceDto>($"children/{childId}/balance");
    }

    public async Task<IReadOnlyList<DeedTypeDto>> GetDeedTypesAsync(Guid parentId)
    {
    var items = await _http.GetFromJsonAsync<List<DeedTypeDto>>($"parents/{parentId}/deed-types");
    return items ?? new List<DeedTypeDto>();
    }

    public async Task<DeedTypeDto> CreateDeedTypeAsync(Guid parentId, string name, int points)
    {
        var response = await _http.PostAsJsonAsync($"parents/{parentId}/deed-types", new CreateDeedTypeRequest(parentId, name, points));
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<DeedTypeDto>())!;
        }

        var error = await ReadErrorAsync(response);
        throw new InvalidOperationException(error ?? "Unable to create deed type");
    }

    public async Task DeleteDeedTypeAsync(Guid parentId, Guid deedTypeId)
    {
        var response = await _http.DeleteAsync($"parents/{parentId}/deed-types/{deedTypeId}");
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            var error = await ReadErrorAsync(response);
            throw new InvalidOperationException(error ?? "Unable to delete deed type");
        }
    }

    public async Task<DeedDto> CreateDeedAsync(Guid childId, Guid deedTypeId, int points, string? note, Guid createdBy)
    {
        var response = await _http.PostAsJsonAsync("deeds", new CreateDeedRequest(childId, deedTypeId, points, note, createdBy));
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadFromJsonAsync<DeedDto>())!;
        }

        var error = await ReadErrorAsync(response);
        throw new InvalidOperationException(error ?? "Unable to create deed");
    }

    public async Task<IReadOnlyList<DeedDto>> GetDeedsForChildAsync(Guid childId)
    {
    var items = await _http.GetFromJsonAsync<List<DeedDto>>($"children/{childId}/deeds");
    return items ?? new List<DeedDto>();
    }

    public async Task DeleteDeedAsync(Guid childId, Guid deedId)
    {
        var response = await _http.DeleteAsync($"children/{childId}/deeds/{deedId}");
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            var error = await ReadErrorAsync(response);
            throw new InvalidOperationException(error ?? "Unable to delete deed");
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return error?.Error;
        }
        catch
        {
            return null;
        }
    }
}
