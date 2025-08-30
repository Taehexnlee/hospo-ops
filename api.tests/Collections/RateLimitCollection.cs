using Xunit;

namespace Api.Tests.Collections
{
    /// <summary>
    /// Collection to serialize/isolates RateLimit tests that may influence global state.
    /// </summary>
    [CollectionDefinition("RateLimitCollection")]
    public class RateLimitCollection : ICollectionFixture<RateLimitFixture> { }

    public class RateLimitFixture { }
}
