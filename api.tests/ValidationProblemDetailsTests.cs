using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

public class ValidationProblemDetailsTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public ValidationProblemDetailsTests(TestFactory f) => _client = f.CreateClient();

    private record EodDto(int StoreId, string BizDate, decimal NetSales);

    [Fact]
    public async Task Eod_Create_With_Negative_NetSales_Returns_400_ProblemDetails()
    {
        // 1) 임의의 매장 id 확보 (응답이 배열 또는 { items: [] } 모두 지원)
        int storeId;
        {
            var list = new HttpRequestMessage(HttpMethod.Get, "/api/stores?page=1&pageSize=1");
            list.Headers.Add("X-Api-Key", "dev-super-secret");
            var lRes = await _client.SendAsync(list);
            lRes.EnsureSuccessStatusCode();

            var json = await lRes.Content.ReadAsStringAsync();
            using var lDoc = JsonDocument.Parse(json);
            var root = lDoc.RootElement;

            JsonElement items = root.ValueKind switch
            {
                JsonValueKind.Array => root,
                JsonValueKind.Object when root.TryGetProperty("items", out var it) => it,
                _ => throw new System.Exception("Unexpected stores list shape")
            };

            storeId = items[0].GetProperty("id").GetInt32();
        }

        // 2) 음수 매출로 생성 시도 -> 400 + problem+json
        var bad = new HttpRequestMessage(HttpMethod.Post, "/api/eod");
        bad.Headers.Add("X-Api-Key", "dev-super-secret");
        bad.Content = JsonContent.Create(new EodDto(storeId, "2025-08-29", -1m));

        var res = await _client.SendAsync(bad);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        // 3) ProblemDetails 직렬화 형태(오브젝트/배열) 모두 허용
        var pjson = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(pjson);

        Assert.True(
            doc.RootElement.ValueKind == JsonValueKind.Object ||
            doc.RootElement.ValueKind == JsonValueKind.Array
        );

        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            Assert.True(
                doc.RootElement.TryGetProperty("title", out _) ||
                doc.RootElement.TryGetProperty("type", out _) ||
                doc.RootElement.TryGetProperty("status", out _)
            );
        }
    }
}
