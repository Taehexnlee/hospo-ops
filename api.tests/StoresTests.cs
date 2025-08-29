using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class StoresTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public StoresTests(TestFactory f) => _client = f.CreateClient();

    public record StoreDto(string Name);

    [Xunit.Fact]
    public async Task Stores_CRUD_And_Conflict()
    {
        // 항상 고유한 이름으로 생성
        var uniqueName = $"Store-A-{System.Guid.NewGuid():N}";

        // Create
        var mk = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mk.Headers.Add("X-Api-Key", "dev-super-secret");
        mk.Content = JsonContent.Create(new StoreDto(uniqueName));
        var r1 = await _client.SendAsync(mk);
        Xunit.Assert.Equal(HttpStatusCode.Created, r1.StatusCode);

        var created = await r1.Content.ReadFromJsonAsync<JsonElement>();
        int id = created.GetProperty("id").GetInt32();

        // Get
        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/stores/{id}");
        get.Headers.Add("X-Api-Key", "dev-super-secret");
        var r2 = await _client.SendAsync(get);
        Xunit.Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        // Update (이름도 고유하게)
        var upd = new HttpRequestMessage(HttpMethod.Put, $"/api/stores/{id}");
        upd.Headers.Add("X-Api-Key", "dev-super-secret");
        upd.Content = JsonContent.Create(new StoreDto($"{uniqueName}-Updated"));
        var r3 = await _client.SendAsync(upd);
        Xunit.Assert.Equal(HttpStatusCode.OK, r3.StatusCode);

        // Duplicate name => 409 (같은 이름으로 다시 생성 시도)
        var dup = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        dup.Headers.Add("X-Api-Key", "dev-super-secret");
        dup.Content = JsonContent.Create(new StoreDto($"{uniqueName}-Updated"));
        var r4 = await _client.SendAsync(dup);
        Xunit.Assert.Equal(HttpStatusCode.Conflict, r4.StatusCode);

        // Delete
        var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/stores/{id}");
        del.Headers.Add("X-Api-Key", "dev-super-secret");
        var r5 = await _client.SendAsync(del);
        Xunit.Assert.Equal(HttpStatusCode.NoContent, r5.StatusCode);
    }
}
