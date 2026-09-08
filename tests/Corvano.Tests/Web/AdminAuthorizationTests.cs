using System.Net;

namespace Corvano.Tests.Web;

[Collection(DatabaseCollection.Name)]
public sealed class AdminAuthorizationTests : IAsyncLifetime
{
    private readonly AdminWebFactory _factory = new();

    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("/admin/products")]
    [InlineData("/admin/categories")]
    public async Task An_anonymous_visitor_is_redirected_to_the_login_page(string url)
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location!;
        var path = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString.Split('?')[0];
        Assert.Equal("/admin/auth/login", path);
    }

    [Fact]
    public async Task The_login_page_itself_is_reachable_without_a_session()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync("/admin/auth/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
