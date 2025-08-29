using System.Net;
using System.Net.Http.Headers;
using Xunit;
using System.Linq;

public class SecurityHeadersAndCorsTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public SecurityHeadersAndCorsTests(TestFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Health_Returns_Security_Headers()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.Contains("X-Content-Type-Options", res.Headers.Select(h => h.Key));
        Assert.Contains("X-Frame-Options", res.Headers.Select(h => h.Key));
        Assert.Contains("Referrer-Policy", res.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task Cors_Allows_Whitelisted_Origin_On_Api()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
        req.Headers.Add("X-Api-Key", "dev-super-secret");
        req.Headers.Add("Origin", "http://localhost:3000");

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var allowOrigin = res.Headers.TryGetValues("Access-Control-Allow-Origin", out var v) ? v.FirstOrDefault() : null;
        Assert.Equal("http://localhost:3000", allowOrigin);
    }

    [Fact]
    public async Task Cors_Does_Not_Reflect_Arbitrary_Origin()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
        req.Headers.Add("X-Api-Key", "dev-super-secret");
        req.Headers.Add("Origin", "http://evil.example.com");

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.False(res.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
