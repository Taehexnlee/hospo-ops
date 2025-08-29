using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class CorrelationIdTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;
    public CorrelationIdTests(TestFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Adds_CorrelationId_Header_On_Response()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(res.Headers.TryGetValues("X-Correlation-Id", out var vals));
        Assert.False(string.IsNullOrWhiteSpace(vals.FirstOrDefault()));
    }

    [Fact]
    public async Task Echoes_Incoming_CorrelationId_When_Provided()
    {
        var cid = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Get, "/health");
        req.Headers.Add("X-Correlation-Id", cid);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(res.Headers.TryGetValues("X-Correlation-Id", out var vals));
        Assert.Equal(cid, vals.FirstOrDefault());
    }
}
