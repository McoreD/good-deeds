using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GoodDeeds.Client.Services;

public record ChatGptSuggestion(int Points, string Reason);

public class ChatGptService
{
    private static readonly Uri OpenAiBaseUri = new("https://api.openai.com/");

    public async Task<ChatGptSuggestion?> SuggestPointsAsync(string apiKey, string deedTypeName, bool isPositive, string description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key is required", nameof(apiKey));
        }

        using var httpClient = new HttpClient { BaseAddress = OpenAiBaseUri };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-4o-mini",
            temperature = 0.3,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = "You help parents score children's deeds. Respond with JSON containing integer 'points' and string 'reason'. Good deeds should have positive points, bad deeds negative." },
                new { role = "user", content = $"Deed type: {deedTypeName}. Nature: {(isPositive ? "good" : "bad")}. Description: {description}." }
            }
        };

        using var response = await httpClient.PostAsJsonAsync("v1/chat/completions", requestBody, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        var content = choices[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        using var suggestionJson = JsonDocument.Parse(content);
        var points = suggestionJson.RootElement.TryGetProperty("points", out var pointsProp) ? pointsProp.GetInt32() : (int?)null;
        var reason = suggestionJson.RootElement.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : null;

        if (points is null || reason is null)
        {
            return null;
        }

        return new ChatGptSuggestion(points.Value, reason);
    }
}
