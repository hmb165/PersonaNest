using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IGoogleBooksService"/>
public class GoogleBooksService : IGoogleBooksService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleBooksService> _logger;

    public GoogleBooksService(
        HttpClient httpClient, IConfiguration configuration, ILogger<GoogleBooksService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(
                configuration["GoogleBooks:BaseUrl"] ?? "https://www.googleapis.com/books/v1/");
        }
    }

    public async Task<IReadOnlyList<ExternalSearchResultDto>> SearchAsync(
        string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ExternalSearchResultDto>();
        }

        try
        {
            var url = $"volumes?q={Uri.EscapeDataString(query)}&maxResults=10";

            var response = await _httpClient.GetFromJsonAsync<GoogleBooksResponse>(url, cancellationToken);
            if (response?.Items is null)
            {
                return Array.Empty<ExternalSearchResultDto>();
            }

            return response.Items
                .Where(i => i.VolumeInfo is not null)
                .Select(i => new ExternalSearchResultDto
                {
                    ExternalId = i.Id ?? string.Empty,
                    Title = i.VolumeInfo!.Title ?? string.Empty,
                    Overview = string.IsNullOrWhiteSpace(i.VolumeInfo.Description) ? null : i.VolumeInfo.Description,
                    // Google Books returns http:// thumbnails - upgrade to https so they don't
                    // get blocked as mixed content on this app's https pages.
                    ImageUrl = i.VolumeInfo.ImageLinks?.Thumbnail?.Replace("http://", "https://"),
                    ReleaseYear = ParseYear(i.VolumeInfo.PublishedDate)
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Google Books search failed for query {Query}.", query);
            return Array.Empty<ExternalSearchResultDto>();
        }
    }

    /// <summary>Google's publishedDate ranges from "2020" to "2020-01-15" - not all of which
    /// DateTime.TryParse accepts, so just read the leading 4-digit year directly.</summary>
    private static int? ParseYear(string? publishedDate) =>
        !string.IsNullOrWhiteSpace(publishedDate) && publishedDate.Length >= 4
        && int.TryParse(publishedDate.AsSpan(0, 4), out var year)
            ? year
            : null;

    private sealed class GoogleBooksResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleBookItem>? Items { get; set; }
    }

    private sealed class GoogleBookItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("volumeInfo")]
        public GoogleBookVolumeInfo? VolumeInfo { get; set; }
    }

    private sealed class GoogleBookVolumeInfo
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }

        [JsonPropertyName("imageLinks")]
        public GoogleBookImageLinks? ImageLinks { get; set; }
    }

    private sealed class GoogleBookImageLinks
    {
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
    }
}
