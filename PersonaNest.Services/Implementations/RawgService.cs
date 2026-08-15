using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IRawgService"/>
public class RawgService : IRawgService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RawgService> _logger;
    private readonly string? _apiKey;

    public RawgService(HttpClient httpClient, IConfiguration configuration, ILogger<RawgService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["Rawg:ApiKey"];

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(configuration["Rawg:BaseUrl"] ?? "https://api.rawg.io/api/");
        }
    }

    public async Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("RAWG search skipped: Rawg:ApiKey is not configured.");
            return Array.Empty<ExternalSearchResultDto>();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ExternalSearchResultDto>();
        }

        try
        {
            var url = $"games?key={Uri.EscapeDataString(_apiKey)}&search={Uri.EscapeDataString(query)}&page_size=10";

            var response = await _httpClient.GetFromJsonAsync<RawgSearchResponse>(url, cancellationToken);
            if (response?.Results is null)
            {
                return Array.Empty<ExternalSearchResultDto>();
            }

            return response.Results
                .Select(r => new ExternalSearchResultDto
                {
                    ExternalId = r.Id.ToString(),
                    Title = r.Name ?? string.Empty,
                    // RAWG's search-list endpoint doesn't include a description - only the
                    // single-game detail endpoint does, and a second call per result isn't worth
                    // it for autofill.
                    Overview = null,
                    ImageUrl = r.BackgroundImage,
                    ReleaseYear = ParseYear(r.Released)
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "RAWG search failed for query {Query}.", query);
            return Array.Empty<ExternalSearchResultDto>();
        }
    }

    private static int? ParseYear(string? isoDate) =>
        !string.IsNullOrWhiteSpace(isoDate) && DateTime.TryParse(isoDate, out var parsed)
            ? parsed.Year
            : null;

    private sealed class RawgSearchResponse
    {
        [JsonPropertyName("results")]
        public List<RawgGame>? Results { get; set; }
    }

    private sealed class RawgGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }

        [JsonPropertyName("released")]
        public string? Released { get; set; }
    }
}
