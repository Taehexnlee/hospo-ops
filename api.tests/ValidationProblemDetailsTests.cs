using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

public class ValidationProblemDetailsTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public ValidationProblemDetailsTests(TestFactory f) => _client = f.CreateClient();

    record EodDto(int StoreId, string BizDate, decimal NetSales);

    [Fact]
    public async Task Eod_Create_With_Negative_NetSales_Returns_400_ProblemDetails()
    {
        // 먼저 유효한 매장 하나 생성
        var mkStore = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mkStore.Headers.Add("X-Api-Key", "dev-super-secret");
        mkStore.Content = JsonContent.Create(new { name = "Val-Store" });
        var sRes = await _client.SendAsync(mkStore);
        Assert.True(sRes.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

        int storeId = 0;
        if (sRes.StatusCode == HttpStatusCode.Created)
        {
            using var sDoc = JsonDocument.Parse(await sRes.Content.ReadAsStringAsync());
            storeId = sDoc.RootElement.GetProperty("id").GetInt32();
        }
        else
        {
            // 이미 존재한다면 하나 조회해서 사용
            var list = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
            list.Headers.Add("X-Api-Key", "dev-super-secret");
            var lRes = await _client.SendAsync(list);
            lRes.EnsureSuccessStatusCode();
            using var lDoc = JsonDocument.Parse(await lRes.Content.ReadAsStringAsync());
            var items = lDoc.RootElement.GetProperty("items");
            storeId = items[0].GetProperty("id").GetInt32();
        }

        // 음수 매출 -> 400 + problem+json
        var bad = new HttpRequestMessage(HttpMethod.Post, "/api/eod");
        bad.Headers.Add("X-Api-Key", "dev-super-secret");
        bad.Content = JsonContent.Create(new EodDto(storeId, "2025-08-29", -1m));

        var res = await _client.SendAsync(bad);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }
}
