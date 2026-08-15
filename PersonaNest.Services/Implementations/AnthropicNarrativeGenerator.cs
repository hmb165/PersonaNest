using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IAiNarrativeGenerator"/>
public class AnthropicNarrativeGenerator : IAiNarrativeGenerator
{
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxNarrativeLength = 600;

    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicNarrativeGenerator> _logger;
    private readonly string? _apiKey;
    private readonly string _model;

    public AnthropicNarrativeGenerator(
        HttpClient httpClient, IConfiguration configuration, ILogger<AnthropicNarrativeGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["Anthropic:ApiKey"];
        _model = configuration["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(configuration["Anthropic:BaseUrl"] ?? "https://api.anthropic.com/v1/");
        }
    }

    public async Task<string?> GenerateAsync(
        string displayName, TasteProfileDto profile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("AI narrative generation skipped: Anthropic:ApiKey is not configured.");
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "messages")
        {
            Content = JsonContent.Create(new AnthropicRequest
            {
                Model = _model,
                MaxTokens = 250,
                Messages = new[] { new AnthropicMessage { Role = "user", Content = BuildPrompt(displayName, profile) } }
            })
        };
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI narrative generation failed: Anthropic returned {StatusCode}.", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken);
            var text = body?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            text = text.Trim();
            return text.Length > MaxNarrativeLength ? text[..MaxNarrativeLength] : text;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "AI narrative generation failed for user {DisplayName}.", displayName);
            return null;
        }
    }

    private static string BuildPrompt(string displayName, TasteProfileDto profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "You are writing a short, warm, second-person summary of a media journal user's taste " +
            "for their dashboard. Write 2-3 sentences, no more than 60 words, no markdown, no " +
            "greeting, no sign-off - just the summary paragraph itself.");
        sb.AppendLine();
        sb.AppendLine($"User: {displayName}");
        sb.AppendLine($"Total entries logged: {profile.TotalEntries}");

        if (profile.AverageRating is { } avg)
        {
            sb.AppendLine($"Average rating given: {avg}/10");
        }

        if (profile.Categories.Count > 0)
        {
            var top = profile.Categories.Take(3)
                .Select(c => $"{c.CategoryName} ({c.Percentage}%)");
            sb.AppendLine($"Top categories: {string.Join(", ", top)}");
        }

        if (profile.TopTags.Count > 0)
        {
            var tags = profile.TopTags.Take(5).Select(t => t.Name);
            sb.AppendLine($"Frequently used tags: {string.Join(", ", tags)}");
        }

        if (profile.MostActiveMonth is { } month)
        {
            sb.AppendLine($"Most active month: {month:MMMM yyyy}");
        }

        return sb.ToString();
    }

    private sealed class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public AnthropicMessage[] Messages { get; set; } = Array.Empty<AnthropicMessage>();
    }

    private sealed class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock>? Content { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
