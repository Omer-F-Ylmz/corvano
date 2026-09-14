using Corvano.Business.Concrete;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class CatalogSeedManagerTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task SeedAsync()
    {
        await using var context = TestDb.NewContext();
        await new CatalogSeedManager(
            new EfCategoryDal(context),
            new EfProductDal(context),
            new EfProductVariantDal(context),
            new EfProductImageDal(context),
            new EfUnitOfWork(context)).EnsureSeedAsync();
    }

    private static async Task<(int Top, int Sub, int Products, int Variants, int Images)> CountAsync(CorvanoContext context)
    {
        var categories = await new EfCategoryDal(context).GetListAsync();
        return (
            categories.Count(c => c.ParentId is null),
            categories.Count(c => c.ParentId is not null),
            (await new EfProductDal(context).GetListAsync()).Count,
            (await new EfProductVariantDal(context).GetListAsync()).Count,
            (await new EfProductImageDal(context).GetListAsync()).Count);
    }

    [Fact]
    public async Task Seeding_twice_writes_each_category_product_variant_and_image_once()
    {
        await SeedAsync();
        await using var context = TestDb.NewContext();
        var first = await CountAsync(context);

        await SeedAsync();
        var second = await CountAsync(context);

        Assert.Equal((5, 3, 12), (first.Top, first.Sub, first.Products));
        Assert.True(first.Variants >= 24, $"her üründe ≥2 varyant beklenir, {first.Variants} var");
        Assert.True(first.Images >= 12, $"her üründe ≥1 görsel beklenir, {first.Images} var");
        Assert.Equal(first, second);
    }
}
