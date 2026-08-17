using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IKitsuService"/>
public class KitsuService : IKitsuService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KitsuService> _logger;

    public KitsuService(HttpClient httpClient, IConfiguration configuration, ILogger<KitsuService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(configuration["Kitsu:BaseUrl"] ?? "https://kitsu.io/api/edge/");
        }

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.api+json");
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
            // "[" / "]" are gen-delims that .NET's Uri won't send un-encoded, so filter[text] and
            // page[limit] are spelled out percent-encoded rather than built with raw brackets.
            var url = $"{mediaType}?filter%5Btext%5D={Uri.EscapeDataString(query)}&page%5Blimit%5D=10";

            var response = await _httpClient.GetFromJsonAsync<KitsuSearchResponse>(url, cancellationToken);
            if (response?.Data is null)
            {
                return Array.Empty<ExternalSearchResultDto>();
            }

            return response.Data
                .Where(i => i.Attributes is not null)
                .Select(i => new ExternalSearchResultDto
                {
                    ExternalId = i.Id ?? string.Empty,
                    Title = i.Attributes!.CanonicalTitle ?? string.Empty,
                    Overview = string.IsNullOrWhiteSpace(i.Attributes.Synopsis) ? null : i.Attributes.Synopsis,
                    ImageUrl = i.Attributes.PosterImage?.Large,
                    ReleaseYear = ParseYear(i.Attributes.StartDate)
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Kitsu search failed for {MediaType} query {Query}.", mediaType, query);
            return Array.Empty<ExternalSearchResultDto>();
        }
    }

    /// <summary>Kitsu's startDate is "YYYY-MM-DD" - read the leading 4-digit year directly rather
    /// than a full DateTime.Parse, matching the other providers' date handling.</summary>
    private static int? ParseYear(string? startDate) =>
        !string.IsNullOrWhiteSpace(startDate) && startDate.Length >= 4
        && int.TryParse(startDate.AsSpan(0, 4), out var year)
            ? year
            : null;

    private sealed class KitsuSearchResponse
    {
        [JsonPropertyName("data")]
        public List<KitsuEntry>? Data { get; set; }
    }

    private sealed class KitsuEntry
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("attributes")]
        public KitsuAttributes? Attributes { get; set; }
    }

    private sealed class KitsuAttributes
    {
        [JsonPropertyName("canonicalTitle")]
        public string? CanonicalTitle { get; set; }

        [JsonPropertyName("synopsis")]
        public string? Synopsis { get; set; }

        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [JsonPropertyName("posterImage")]
        public KitsuPosterImage? PosterImage { get; set; }
    }

    private sealed class KitsuPosterImage
    {
        [JsonPropertyName("large")]
        public string? Large { get; set; }
    }
}
