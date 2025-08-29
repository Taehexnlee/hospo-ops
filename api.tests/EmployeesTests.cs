using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class EmployeesTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public EmployeesTests(TestFactory f) => _client = f.CreateClient();

    public record StoreDto(string Name);
    public record EmpDto(int StoreId, string FullName, string Role, string? HireDate, bool Active);

    [Xunit.Fact]
    public async Task Employees_CRUD_And_Validation()
    {
        // 준비: Store 생성
        var mkStore = new HttpRequestMessage(HttpMethod.Post, "/api/stores");
        mkStore.Headers.Add("X-Api-Key", "dev-super-secret");
        mkStore.Content = JsonContent.Create(new StoreDto($"Test-Store-{System.Guid.NewGuid():N}"));
        var storeRes = await _client.SendAsync(mkStore);
        Xunit.Assert.Equal(HttpStatusCode.Created, storeRes.StatusCode);
        var storeJson = await storeRes.Content.ReadFromJsonAsync<JsonElement>();
        int storeId = storeJson.GetProperty("id").GetInt32();

        // Create
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/employees");
        create.Headers.Add("X-Api-Key", "dev-super-secret");
        create.Content = JsonContent.Create(new EmpDto(storeId, "Test User", "Barista", "2024-01-01", true));
        var cRes = await _client.SendAsync(create);
        Xunit.Assert.Equal(HttpStatusCode.Created, cRes.StatusCode);
        var empJson = await cRes.Content.ReadFromJsonAsync<JsonElement>();
        int empId = empJson.GetProperty("id").GetInt32();

        // Duplicate in same store => 409
        var dup = new HttpRequestMessage(HttpMethod.Post, "/api/employees");
        dup.Headers.Add("X-Api-Key", "dev-super-secret");
        dup.Content = JsonContent.Create(new EmpDto(storeId, "Test User", "Barista", "2024-01-01", true));
        var dupRes = await _client.SendAsync(dup);
        Xunit.Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);

        // Get
        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/employees/{empId}");
        get.Headers.Add("X-Api-Key", "dev-super-secret");
        var gRes = await _client.SendAsync(get);
        Xunit.Assert.Equal(HttpStatusCode.OK, gRes.StatusCode);

        // Update
        var update = new HttpRequestMessage(HttpMethod.Put, $"/api/employees/{empId}");
        update.Headers.Add("X-Api-Key", "dev-super-secret");
        update.Content = JsonContent.Create(new EmpDto(storeId, "Test User", "Supervisor", "2024-06-01", true));
        var uRes = await _client.SendAsync(update);
        Xunit.Assert.Equal(HttpStatusCode.OK, uRes.StatusCode);

        // Delete
        var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/employees/{empId}");
        del.Headers.Add("X-Api-Key", "dev-super-secret");
        var dRes = await _client.SendAsync(del);
        Xunit.Assert.Equal(HttpStatusCode.NoContent, dRes.StatusCode);
    }

    [Xunit.Fact]
    public async Task Create_Employee_With_Invalid_StoreId_400()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/employees");
        req.Headers.Add("X-Api-Key", "dev-super-secret");
        req.Content = JsonContent.Create(new EmpDto(0, "Bad User", "Tester", "2025/01/01", true));
        var res = await _client.SendAsync(req);
        Xunit.Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
