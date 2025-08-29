using System.Net;
using System.Net.Http.Json;
using Xunit;

public class EmployeesValidationTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public EmployeesValidationTests(TestFactory f) => _client = f.CreateClient();

    record EmpDto(int StoreId, string FullName, string Role, string? HireDate, bool Active);

    [Fact]
    public async Task Create_Employee_With_Invalid_StoreId_Returns_400()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/employees");
        req.Headers.Add("X-Api-Key", "dev-super-secret");
        req.Content = JsonContent.Create(new EmpDto(0, "Bad User", "Tester", "2025/01/01", true));
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
