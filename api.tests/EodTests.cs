using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

public class EodTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public EodTests(TestFactory f) => _client = f.CreateClient();

    record StoreDto(string Name);
    record EodDto(int StoreId, string BizDate, decimal NetSales);

    [Fact]
    public async Task Create_And_Get_EodReport_Works()
    {
        // 1) 스토어 생성
        var mkStore = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mkStore.Headers.Add("X-Api-Key", "dev-super-secret");
        var uniqueStore = $"Store-{Guid.NewGuid():N}".Substring(0, 12);
        mkStore.Content = JsonContent.Create(new StoreDto(uniqueStore));
        var storeRes = await _client.SendAsync(mkStore);
        Assert.Equal(HttpStatusCode.Created, storeRes.StatusCode);

        using var storeDoc = JsonDocument.Parse(await storeRes.Content.ReadAsStringAsync());
        int storeId = storeDoc.RootElement.GetProperty("id").GetInt32();

        // 2) 매번 고유한 BizDate 사용 (유니크 제약 회피)
        var uniqueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(Environment.TickCount64 % 3000));
        var bizDateStr = uniqueDate.ToString("yyyy-MM-dd");

        // 3) EOD 생성
        var mkEod = new HttpRequestMessage(HttpMethod.Post, "/api/eod");
        mkEod.Headers.Add("X-Api-Key", "dev-super-secret");
        mkEod.Content = JsonContent.Create(new EodDto(storeId, bizDateStr, 123.45m));
        var eodRes = await _client.SendAsync(mkEod);
        Assert.Equal(HttpStatusCode.Created, eodRes.StatusCode);

        using var eodDoc = JsonDocument.Parse(await eodRes.Content.ReadAsStringAsync());
        long eodId = eodDoc.RootElement.GetProperty("id").GetInt64();

        // 4) 생성된 항목 조회
        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/eod/{eodId}");
        get.Headers.Add("X-Api-Key", "dev-super-secret");
        var gRes = await _client.SendAsync(get);
        Assert.Equal(HttpStatusCode.OK, gRes.StatusCode);
    }
}
