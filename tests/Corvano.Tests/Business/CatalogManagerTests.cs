using System.Net;
using Corvano.Business.Concrete;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class CatalogManagerTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await TestDb.ResetAsync();
        await CatalogFixture.SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CatalogManager NewService(CorvanoContext context)
        => new(new EfCategoryDal(context), new EfProductDal(context), new EfProductVariantDal(context), new EfProductImageDal(context));

    [Fact]
    public async Task A_collection_lists_only_active_products_from_its_subcategories()
    {
        await using var context = TestDb.NewContext();

        var (status, result) = await NewService(context).GetCollectionAsync("aksesuar", subcategorySlug: null);

        Assert.Equal(HttpStatusCode.OK, status);
        var slugs = result.Data!.Products.Select(card => card.Product.Slug).OrderBy(s => s).ToList();
        Assert.Equal([CatalogFixture.Boutonniere, CatalogFixture.ActiveTie], slugs);
    }

    [Fact]
    public async Task A_subcategory_filter_returns_only_that_subcategory_products()
    {
        await using var context = TestDb.NewContext();

        var (status, result) = await NewService(context).GetCollectionAsync("aksesuar", subcategorySlug: "kravat");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("kravat", result.Data!.ActiveSubcategory!.Slug);
        var card = Assert.Single(result.Data.Products);
        Assert.Equal(CatalogFixture.ActiveTie, card.Product.Slug);
        Assert.Equal(["yaka-cicegi", "kravat"], result.Data.Subcategories.Select(c => c.Slug));
    }
}
