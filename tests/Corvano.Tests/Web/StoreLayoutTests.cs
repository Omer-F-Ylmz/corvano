using System.Net;
using System.Text.RegularExpressions;

namespace Corvano.Tests.Web;

[Collection(DatabaseCollection.Name)]
public sealed class StoreLayoutTests : IAsyncLifetime
{
    private readonly AdminWebFactory _factory = new();

    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Section(string html, string marker)
    {
        var match = Regex.Match(html, $"<(nav|section)[^>]*{marker}[^>]*>(.*?)</\\1>", RegexOptions.Singleline);
        Assert.True(match.Success, $"'{marker}' bölümü bulunamadı.");
        return match.Groups[2].Value;
    }

    [Fact]
    public async Task The_top_bar_menu_lists_active_categories_from_the_database_in_sort_order()
    {
        await CatalogFixture.AddCategoryAsync("Gömlek", "gomlek", sortOrder: 2);
        await CatalogFixture.AddCategoryAsync("Pantolon", "pantolon", sortOrder: 1);
        await CatalogFixture.AddCategoryAsync("Gizli Seri", "gizli-seri", sortOrder: 3, isActive: false);

        var html = await _factory.CreateClient().GetStringAsync("/");
        var menu = Section(html, "data-site-menu");

        Assert.Equal(["/koleksiyon/pantolon", "/koleksiyon/gomlek"],
            Regex.Matches(menu, "href=\"(/koleksiyon/[^\"?]+)\"").Select(m => m.Groups[1].Value));
        Assert.DoesNotContain("Gizli Seri", html);
    }

    [Fact]
    public async Task The_home_page_features_eight_active_products()
    {
        var shirts = await CatalogFixture.AddCategoryAsync("Gömlek", "gomlek", 1);
        for (var i = 1; i <= 10; i++)
        {
            await CatalogFixture.AddProductAsync(shirts, $"Gömlek {i}", $"gomlek-{i}", ageMinutes: i);
        }

        await CatalogFixture.AddProductAsync(shirts, "Pasif Gömlek", "pasif-gomlek", isActive: false);

        var html = await _factory.CreateClient().GetStringAsync("/");
        var featured = Section(html, "data-featured");

        Assert.Equal(8, Regex.Matches(featured, "data-product-card").Count);
        Assert.DoesNotContain("pasif-gomlek", featured);
    }

    [Fact]
    public async Task An_empty_collection_says_it_is_being_prepared()
    {
        await CatalogFixture.AddCategoryAsync("Triko", "triko", 1);

        var response = await _factory.CreateClient().GetAsync("/koleksiyon/triko");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Bu koleksiyon hazırlanıyor", await response.Content.ReadAsStringAsync());
    }
}
