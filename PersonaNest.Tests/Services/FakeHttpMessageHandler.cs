namespace PersonaNest.Tests.Services;

/// <summary>Shared HttpClient stand-in for TMDB/Anthropic tests - no real network call, no
/// mocking framework needed since HttpMessageHandler is a plain abstract class.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public static FakeHttpMessageHandler Throwing<TException>() where TException : Exception, new()
        => new(_ => throw new TException());

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_responder(request));
}
