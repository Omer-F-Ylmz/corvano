using System.Net;

namespace Corvano.Tests.Web;

[Collection(DatabaseCollection.Name)]
public sealed class StorefrontTests : IAsyncLifetime
{
    private readonly AdminWebFactory _factory = new();

    public async Task InitializeAsync()
    {
        await TestDb.ResetAsync();
        await CatalogFixture.SeedAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task An_active_product_page_renders_and_a_passive_one_is_not_found()
    {
        var client = _factory.CreateClient();

        var active = await client.GetAsync($"/urun/{CatalogFixture.ActiveTie}");
        var passive = await client.GetAsync($"/urun/{CatalogFixture.PassiveTie}");

        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        Assert.Contains("Kiremit Kravat", await active.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, passive.StatusCode);
    }

    [Theory]
    [InlineData("/urun/boyle-bir-urun-yok")]
    [InlineData("/koleksiyon/boyle-bir-koleksiyon-yok")]
    public async Task An_unknown_slug_is_not_found(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
