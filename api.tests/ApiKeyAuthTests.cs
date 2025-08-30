using System.Net;
using System.Net.Http.Json;
using Xunit;

public class ApiKeyAuthTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public ApiKeyAuthTests(TestFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Blocks_Without_Key()
    {
        var res = await _client.GetAsync("/api/stores?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Blocks_With_Wrong_Key()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
        req.Headers.Add("X-Api-Key", "wrong-key");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Allows_With_Correct_Key()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
        req.Headers.Add("X-Api-Key", "dev-super-secret");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Health_Anonymous_OK()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
