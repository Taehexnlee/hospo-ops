using System.Net;
using System.Net.Http.Json;
using Xunit;
using System;

public class StoresConflictTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public StoresConflictTests(TestFactory f) => _client = f.CreateClient();

    record StoreDto(string Name);

    [Fact]
    public async Task Creating_Duplicate_StoreName_Returns_409()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"DupTest_{suffix}";

        // 1) 첫 생성 -> 201 Created
        var mk1 = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mk1.Headers.Add("X-Api-Key", "dev-super-secret");
        mk1.Content = JsonContent.Create(new StoreDto(name));
        var r1 = await _client.SendAsync(mk1);
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);

        // 2) 같은 이름 재생성 -> 409 Conflict
        var mk2 = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mk2.Headers.Add("X-Api-Key", "dev-super-secret");
        mk2.Content = JsonContent.Create(new StoreDto(name));
        var r2 = await _client.SendAsync(mk2);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }
}