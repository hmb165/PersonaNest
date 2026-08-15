using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="ITmdbService"/>
public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbService> _logger;
    private readonly string? _apiKey;
    private readonly string _imageBaseUrl;

    public TmdbService(HttpClient httpClient, IConfiguration configuration, ILogger<TmdbService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["TMDb:ApiKey"];
        _imageBaseUrl = configuration["TMDb:ImageBaseUrl"] ?? "https://image.tmdb.org/t/p/w342";

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(configuration["TMDb:BaseUrl"] ?? "https://api.themoviedb.org/3/");
        }
    }

    public async Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, string mediaType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("TMDB search skipped: TMDb:ApiKey is not configured.");
            return Array.Empty<ExternalSearchResultDto>();
        }

        if (string.IsNullOrWhiteSpace(query) || mediaType is not ("movie" or "tv"))
        {
            return Array.Empty<ExternalSearchResultDto>();
        }

        try
        {
            var url = $"search/{mediaType}?api_key={Uri.EscapeDataString(_apiKey)}" +
                      $"&query={Uri.EscapeDataString(query)}&include_adult=false";

            var response = await _httpClient.GetFromJsonAsync<TmdbSearchResponse>(url, cancellationToken);
            if (response?.Results is null)
            {
                return Array.Empty<ExternalSearchResultDto>();
            }

            return response.Results
                .Take(10)
                .Select(r => new ExternalSearchResultDto
                {
                    ExternalId = r.Id.ToString(),
                    Title = (mediaType == "movie" ? r.Title : r.Name) ?? string.Empty,
                    Overview = string.IsNullOrWhiteSpace(r.Overview) ? null : r.Overview,
                    ImageUrl = string.IsNullOrWhiteSpace(r.PosterPath)
                        ? null : _imageBaseUrl.TrimEnd('/') + r.PosterPath,
                    ReleaseYear = ParseYear(mediaType == "movie" ? r.ReleaseDate : r.FirstAirDate)
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            // Network hiccup or TMDB outage - the Add Media form still works manually, so this
            // degrades to "no results" rather than a 500.
            _logger.LogWarning(ex, "TMDB search failed for {MediaType} query {Query}.", mediaType, query);
            return Array.Empty<ExternalSearchResultDto>();
        }
    }

    private static int? ParseYear(string? isoDate) =>
        !string.IsNullOrWhiteSpace(isoDate) && DateTime.TryParse(isoDate, out var parsed)
            ? parsed.Year
            : null;

    private sealed class TmdbSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbSearchResultItem>? Results { get; set; }
    }

    private sealed class TmdbSearchResultItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
    }
}
