using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IJikanService"/>
public class JikanService : IJikanService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JikanService> _logger;

    public JikanService(HttpClient httpClient, IConfiguration configuration, ILogger<JikanService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(configuration["Jikan:BaseUrl"] ?? "https://api.jikan.moe/v4/");
        }
    }

    public async Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, string mediaType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || mediaType is not ("anime" or "manga"))
        {
            return Array.Empty<ExternalSearchResultDto>();
        }

        try
        {
            var url = $"{mediaType}?q={Uri.EscapeDataString(query)}&limit=10";

            var response = await _httpClient.GetFromJsonAsync<JikanSearchResponse>(url, cancellationToken);
            if (response?.Data is null)
            {
                return Array.Empty<ExternalSearchResultDto>();
            }

            return response.Data
                .Select(r => new ExternalSearchResultDto
                {
                    ExternalId = r.MalId.ToString(),
                    Title = r.Title ?? string.Empty,
                    Overview = string.IsNullOrWhiteSpace(r.Synopsis) ? null : r.Synopsis,
                    ImageUrl = r.Images?.Jpg?.ImageUrl,
                    ReleaseYear = r.Aired?.Prop?.From?.Year ?? r.Published?.Prop?.From?.Year
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Jikan search failed for {MediaType} query {Query}.", mediaType, query);
            return Array.Empty<ExternalSearchResultDto>();
        }
    }

    private sealed class JikanSearchResponse
    {
        [JsonPropertyName("data")]
        public List<JikanEntry>? Data { get; set; }
    }

    private sealed class JikanEntry
    {
        [JsonPropertyName("mal_id")]
        public int MalId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("synopsis")]
        public string? Synopsis { get; set; }

        [JsonPropertyName("images")]
        public JikanImages? Images { get; set; }

        [JsonPropertyName("aired")]
        public JikanDateRange? Aired { get; set; }

        [JsonPropertyName("published")]
        public JikanDateRange? Published { get; set; }
    }

    private sealed class JikanImages
    {
        [JsonPropertyName("jpg")]
        public JikanImageSet? Jpg { get; set; }
    }

    private sealed class JikanImageSet
    {
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }

    private sealed class JikanDateRange
    {
        [JsonPropertyName("prop")]
        public JikanDateProp? Prop { get; set; }
    }

    private sealed class JikanDateProp
    {
        [JsonPropertyName("from")]
        public JikanDateParts? From { get; set; }
    }

    private sealed class JikanDateParts
    {
        [JsonPropertyName("year")]
        public int? Year { get; set; }
    }
}
