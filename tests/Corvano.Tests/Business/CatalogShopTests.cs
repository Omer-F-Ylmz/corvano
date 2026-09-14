using System.Net;
using Corvano.Business.Concrete;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Dtos;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class CatalogShopTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static CatalogManager NewService(CorvanoContext context)
        => new(new EfCategoryDal(context), new EfProductDal(context), new EfProductVariantDal(context), new EfProductImageDal(context));

    [Theory]
    [InlineData(CollectionSort.Newest, "b-gomlek,c-gomlek,a-gomlek")]
    [InlineData(CollectionSort.PriceAscending, "b-gomlek,a-gomlek,c-gomlek")]
    [InlineData(CollectionSort.PriceDescending, "c-gomlek,a-gomlek,b-gomlek")]
    public async Task A_collection_is_sorted_on_the_server(CollectionSort sort, string expected)
    {
        var shirts = await CatalogFixture.AddCategoryAsync("Gömlek", "gomlek", 1);
        await CatalogFixture.AddProductAsync(shirts, "A Gömlek", "a-gomlek", price: 1500m, ageMinutes: 30);
        await CatalogFixture.AddProductAsync(shirts, "B Gömlek", "b-gomlek", price: 900m, ageMinutes: 10);
        await CatalogFixture.AddProductAsync(shirts, "C Gömlek", "c-gomlek", price: 2100m, ageMinutes: 20);
        await using var context = TestDb.NewContext();

        var (status, result) = await NewService(context).GetCollectionAsync("gomlek", subcategorySlug: null, sort);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(expected, string.Join(',', result.Data!.Products.Select(card => card.Product.Slug)));
    }

    [Fact]
    public async Task Similar_products_come_from_the_same_category_exclude_the_product_and_stop_at_four()
    {
        var shirts = await CatalogFixture.AddCategoryAsync("Gömlek", "gomlek", 1);
        var trousers = await CatalogFixture.AddCategoryAsync("Pantolon", "pantolon", 2);
        for (var i = 1; i <= 6; i++)
        {
            await CatalogFixture.AddProductAsync(shirts, $"Gömlek {i}", $"gomlek-{i}", ageMinutes: i);
        }

        await CatalogFixture.AddProductAsync(shirts, "Pasif Gömlek", "pasif-gomlek", isActive: false);
        await CatalogFixture.AddProductAsync(trousers, "Chino", "chino");
        await using var context = TestDb.NewContext();

        var (status, result) = await NewService(context).GetSimilarAsync("gomlek-1", count: 4);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(4, result.Data!.Count);
        Assert.DoesNotContain(result.Data, card => card.Product.Slug == "gomlek-1");
        Assert.All(result.Data, card =>
        {
            Assert.Equal(shirts, card.Product.CategoryId);
            Assert.True(card.Product.IsActive);
        });
    }
}
