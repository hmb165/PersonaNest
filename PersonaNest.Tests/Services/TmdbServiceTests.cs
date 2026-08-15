using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PersonaNest.Services.Implementations;

namespace PersonaNest.Tests.Services;

/// <summary>
/// Tests for <see cref="TmdbService"/> (bonus: Consume an External API) - the "no API key
/// configured" degrade-gracefully path, and the actual TMDB JSON-to-DTO mapping, using a fake
/// HttpMessageHandler instead of a real network call.
/// </summary>
public class TmdbServiceTests
{
    private static IConfiguration ConfigWithKey(string? apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey is null
                ? Array.Empty<KeyValuePair<string, string?>>()
                : new[] { new KeyValuePair<string, string?>("TMDb:ApiKey", apiKey) })
            .Build();

    private static TmdbService NewService(
        IConfiguration configuration, HttpMessageHandler? handler = null)
    {
        var client = new HttpClient(handler ?? new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));
        return new TmdbService(client, configuration, Mock.Of<ILogger<TmdbService>>());
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenApiKeyNotConfigured()
    {
        var service = NewService(ConfigWithKey(null));

        var result = await service.SearchAsync("Spirited Away", "movie");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("", "movie")]
    [InlineData("Spirited Away", "album")] // unsupported media type
    public async Task SearchAsync_ReturnsEmpty_ForInvalidInput(string query, string mediaType)
    {
        var service = NewService(ConfigWithKey("test-key"));

        var result = await service.SearchAsync(query, mediaType);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_MapsMovieResults_IncludingPosterUrlAndReleaseYear()
    {
        const string json = """
            {
              "results": [
                { "id": 129, "title": "Spirited Away", "overview": "A girl enters a spirit world.",
                  "poster_path": "/abc123.jpg", "release_date": "2001-07-20" }
              ]
            }
            """;
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        var service = NewService(ConfigWithKey("test-key"), handler);

        var results = await service.SearchAsync("Spirited Away", "movie");

        var result = Assert.Single(results);
        Assert.Equal("129", result.ExternalId);
        Assert.Equal("Spirited Away", result.Title);
        Assert.Equal("A girl enters a spirit world.", result.Overview);
        Assert.Equal(2001, result.ReleaseYear);
        Assert.EndsWith("/abc123.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task SearchAsync_UsesNameAndFirstAirDate_ForTvMediaType()
    {
        const string json = """
            {
              "results": [
                { "id": 456, "name": "Attack on Titan", "overview": "Titans.",
                  "poster_path": "/xyz.jpg", "first_air_date": "2013-04-07" }
              ]
            }
            """;
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        var service = NewService(ConfigWithKey("test-key"), handler);

        var results = await service.SearchAsync("Attack on Titan", "tv");

        var result = Assert.Single(results);
        Assert.Equal("Attack on Titan", result.Title);
        Assert.Equal(2013, result.ReleaseYear);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenTheHttpCallFails()
    {
        var handler = FakeHttpMessageHandler.Throwing<HttpRequestException>();
        var service = NewService(ConfigWithKey("test-key"), handler);

        var results = await service.SearchAsync("Spirited Away", "movie");

        Assert.Empty(results);
    }
}
