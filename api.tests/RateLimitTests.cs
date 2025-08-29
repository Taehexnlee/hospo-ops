using System.Net;
using System.Net.Http.Json;
using Xunit;

public class RateLimitTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public RateLimitTests(TestFactory f) => _client = f.CreateClient();

    record StoreDto(string Name);

    [Fact]
    public async Task Burst_Requests_Hit_429()
    {
        // 사전 생성(정책 적용되는 경로)
        var mk = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mk.Headers.Add("X-Api-Key", "dev-super-secret");
        mk.Content = JsonContent.Create(new StoreDto($"RL-{Guid.NewGuid():N}"));
        var first = await _client.SendAsync(mk);
        Assert.True(first.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

        // 연속 조회로 429 유도
        HttpStatusCode last = 0;
        for (int i = 0; i < 60; i++)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
            req.Headers.Add("X-Api-Key", "dev-super-secret");
            var res = await _client.SendAsync(req);
            last = res.StatusCode;
            if ((int)last == 429) break;
        }
        Assert.Equal((HttpStatusCode)429, last);
    }

    [Fact]
    public async Task Health_And_Swagger_Not_RateLimited()
    {
        for (int i = 0; i < 50; i++)
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/swagger/index.html")).StatusCode);
    }
}
