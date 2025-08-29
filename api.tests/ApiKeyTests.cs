using System.Net;
using System.Net.Http;

public class ApiKeyTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public ApiKeyTests(TestFactory f) => _client = f.CreateClient();

    [Xunit.Fact]
    public async Task Eod_List_Without_ApiKey_401()
    {
        var res = await _client.GetAsync("/api/eod?page=1&pageSize=1");
        Xunit.Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Xunit.Fact]
    public async Task Eod_List_With_ApiKey_200()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/eod?page=1&pageSize=1");
        req.Headers.Add("X-Api-Key", "dev-super-secret");
        var res = await _client.SendAsync(req);
        Xunit.Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Xunit.Fact]
    public async Task Health_And_Swagger_DoNot_Require_ApiKey()
    {
        var h = await _client.GetAsync("/health");
        Xunit.Assert.Equal(HttpStatusCode.OK, h.StatusCode);

        var sw = await _client.GetAsync("/swagger/index.html");
        Xunit.Assert.Equal(HttpStatusCode.OK, sw.StatusCode);
    }
}
