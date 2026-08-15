using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Interfaces;

namespace PersonaNest.Services.Implementations;

/// <inheritdoc cref="IMusicBrainzService"/>
public class MusicBrainzService : IMusicBrainzService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MusicBrainzService> _logger;

    public MusicBrainzService(
        HttpClient httpClient, IConfiguration configuration, ILogger<MusicBrainzService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(
                configuration["MusicBrainz:BaseUrl"] ?? "https://musicbrainz.org/ws/2/");
        }

        // Required by MusicBrainz's API usage policy - generic/missing User-Agent requests get
        // throttled or rejected.
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "PersonaNest/1.0 (student capstone project)");
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
            var url = $"release-group/?query={Uri.EscapeDataString(query)}&fmt=json&limit=10";

            var response = await _httpClient.GetFromJsonAsync<MusicBrainzResponse>(url, cancellationToken);
            if (response?.ReleaseGroups is null)
            {
                return Array.Empty<ExternalSearchResultDto>();
            }

            return response.ReleaseGroups
                .Select(r => new ExternalSearchResultDto
                {
                    ExternalId = r.Id ?? string.Empty,
                    Title = r.Title ?? string.Empty,
                    Overview = r.ArtistCredit is { Count: > 0 }
                        ? "By " + string.Join(", ", r.ArtistCredit.Select(a => a.Name))
                        : null,
                    // Cover Art Archive's convention URL - works without a separate lookup call,
                    // but 404s for release groups with no art; the Add Media form just shows no
                    // preview image in that case rather than failing the whole search.
                    ImageUrl = string.IsNullOrWhiteSpace(r.Id)
                        ? null
                        : $"https://coverartarchive.org/release-group/{r.Id}/front-250",
                    ReleaseYear = ParseYear(r.FirstReleaseDate)
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogWarning(ex, "MusicBrainz search failed for query {Query}.", query);
            return Array.Empty<ExternalSearchResultDto>();
        }
    }

    private static int? ParseYear(string? isoDate) =>
        !string.IsNullOrWhiteSpace(isoDate) && isoDate.Length >= 4
        && int.TryParse(isoDate.AsSpan(0, 4), out var year)
            ? year
            : null;

    private sealed class MusicBrainzResponse
    {
        [JsonPropertyName("release-groups")]
        public List<MusicBrainzReleaseGroup>? ReleaseGroups { get; set; }
    }

    private sealed class MusicBrainzReleaseGroup
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("first-release-date")]
        public string? FirstReleaseDate { get; set; }

        [JsonPropertyName("artist-credit")]
        public List<MusicBrainzArtistCredit>? ArtistCredit { get; set; }
    }

    private sealed class MusicBrainzArtistCredit
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
