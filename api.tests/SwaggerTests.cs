using System.Net;
using System.Text.Json;
using Xunit;

public class SwaggerTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public SwaggerTests(TestFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task SwaggerJson_Exposes_ApiKey_SecurityScheme()
    {
        var res = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("components", out var comps));
        Assert.True(comps.TryGetProperty("securitySchemes", out var schemes));
        Assert.True(schemes.TryGetProperty("ApiKey", out var apiKeyScheme));

        Assert.Equal("apiKey", apiKeyScheme.GetProperty("type").GetString());
        Assert.Equal("header", apiKeyScheme.GetProperty("in").GetString());
        Assert.Equal("X-Api-Key", apiKeyScheme.GetProperty("name").GetString());
    }

    [Fact]
    public async Task SwaggerUI_Loads_In_Dev()
    {
        var res = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.True(html.Contains("hospo-ops") || html.Contains("Swagger UI") || html.Contains("swagger-ui"));
}
}
