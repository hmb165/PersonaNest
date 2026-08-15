using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PersonaNest.Services.DTOs.Responses;
using PersonaNest.Services.Implementations;

namespace PersonaNest.Tests.Services;

/// <summary>
/// Tests for <see cref="AnthropicNarrativeGenerator"/> (bonus: AI) - the "no API key configured"
/// degrade-gracefully path (must never throw and break the taste-profile refresh cycle that calls
/// it), and response parsing, using a fake HttpMessageHandler instead of a real network call.
/// </summary>
public class AnthropicNarrativeGeneratorTests
{
    private static readonly TasteProfileDto SampleProfile = new()
    {
        AverageRating = 8.5m,
        TotalEntries = 12,
        Categories = new[]
        {
            new TasteCategorySliceDto { CategoryId = 1, CategoryName = "Games", Percentage = 60m }
        },
        TopTags = new[] { new TasteTagDto { TagId = 1, Name = "RPG", UseCount = 5 } }
    };

    private static IConfiguration ConfigWithKey(string? apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey is null
                ? Array.Empty<KeyValuePair<string, string?>>()
                : new[] { new KeyValuePair<string, string?>("Anthropic:ApiKey", apiKey) })
            .Build();

    private static AnthropicNarrativeGenerator NewGenerator(
        IConfiguration configuration, HttpMessageHandler? handler = null)
    {
        var client = new HttpClient(handler ?? new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));
        return new AnthropicNarrativeGenerator(
            client, configuration, Mock.Of<ILogger<AnthropicNarrativeGenerator>>());
    }

    [Fact]
    public async Task GenerateAsync_ReturnsNull_WhenApiKeyNotConfigured()
    {
        var generator = NewGenerator(ConfigWithKey(null));

        var result = await generator.GenerateAsync("Alice", SampleProfile);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsGeneratedText_OnSuccessfulResponse()
    {
        const string json = """
            {
              "content": [ { "type": "text", "text": "Alice gravitates toward story-driven RPGs." } ]
            }
            """;
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        var generator = NewGenerator(ConfigWithKey("test-key"), handler);

        var result = await generator.GenerateAsync("Alice", SampleProfile);

        Assert.Equal("Alice gravitates toward story-driven RPGs.", result);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsNull_OnNonSuccessStatusCode()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"invalid api key\"}")
            });
        var generator = NewGenerator(ConfigWithKey("bad-key"), handler);

        var result = await generator.GenerateAsync("Alice", SampleProfile);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsNull_WhenTheHttpCallFails()
    {
        var handler = FakeHttpMessageHandler.Throwing<HttpRequestException>();
        var generator = NewGenerator(ConfigWithKey("test-key"), handler);

        var result = await generator.GenerateAsync("Alice", SampleProfile);

        Assert.Null(result);
    }
}
