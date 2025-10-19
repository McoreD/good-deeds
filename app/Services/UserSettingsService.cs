using Microsoft.JSInterop;

namespace GoodDeeds.Client.Services;

public class UserSettingsService
{
    private const string ParentIdKey = "good-deeds.parent-id";
    private const string ChatGptKey = "good-deeds.chatgpt-key";

    private readonly IJSRuntime _jsRuntime;
    private Guid? _cachedParentId;
    private string? _cachedChatGptKey;

    public UserSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<Guid?> GetParentIdAsync()
    {
        if (_cachedParentId.HasValue)
        {
            return _cachedParentId.Value;
        }

        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ParentIdKey);
        if (Guid.TryParse(value, out var id))
        {
            _cachedParentId = id;
            return id;
        }

        return null;
    }

    public async Task SetParentIdAsync(Guid id)
    {
        _cachedParentId = id;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ParentIdKey, id.ToString());
    }

    public async Task ClearParentIdAsync()
    {
        _cachedParentId = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ParentIdKey);
    }

    public async Task<string?> GetChatGptKeyAsync()
    {
        if (_cachedChatGptKey is not null)
        {
            return _cachedChatGptKey;
        }

        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ChatGptKey);
        _cachedChatGptKey = string.IsNullOrWhiteSpace(value) ? null : value;
        return _cachedChatGptKey;
    }

    public async Task SetChatGptKeyAsync(string? key)
    {
        _cachedChatGptKey = string.IsNullOrWhiteSpace(key) ? null : key?.Trim();
        if (_cachedChatGptKey is null)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ChatGptKey);
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ChatGptKey, _cachedChatGptKey);
        }
    }
}
