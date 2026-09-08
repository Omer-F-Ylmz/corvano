using System.Net;
using Corvano.Business.Concrete;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class ProductManagerTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static ProductManager NewService(CorvanoContext context)
        => new(new EfProductDal(context), new EfProductVariantDal(context), new EfProductImageDal(context), new EfUnitOfWork(context));

    private static async Task<int> SeedCategoryAsync(CorvanoContext context)
    {
        var category = new Category { Name = "Gömlek", Slug = "gomlek", SortOrder = 1, IsActive = true };
        await new EfCategoryDal(context).AddAsync(category);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return category.Id;
    }

    private static Product NewProduct(int categoryId, string name) => new()
    {
        Name = name,
        Description = "Pamuk karışımı.",
        CategoryId = categoryId,
        Price = 1290m,
        IsActive = true
    };

    [Fact]
    public async Task Adding_a_product_derives_its_slug_from_the_name()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var service = NewService(context);

        var (status, result) = await service.AddAsync(NewProduct(categoryId, "Keten Gömlek"));

        Assert.Equal(HttpStatusCode.Created, status);
        Assert.Equal("keten-gomlek", result.Data!.Slug);
    }

    [Fact]
    public async Task Adding_a_second_product_with_the_same_name_suffixes_the_slug()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var service = NewService(context);

        await service.AddAsync(NewProduct(categoryId, "Keten Gömlek"));
        var (_, second) = await service.AddAsync(NewProduct(categoryId, "Keten Gömlek"));

        Assert.Equal("keten-gomlek-2", second.Data!.Slug);
    }

    [Fact]
    public async Task Adding_a_variant_with_negative_stock_is_rejected()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var service = NewService(context);
        var (_, created) = await service.AddAsync(NewProduct(categoryId, "Keten Gömlek"));
        var productId = created.Data!.Id;

        var (status, result) = await service.AddVariantAsync(new ProductVariant
        {
            ProductId = productId,
            Size = "M",
            Color = "Ekru",
            Sku = "CRV-KG-M-EKR",
            Stock = -1
        });

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.False(result.Success);
        var (_, variants) = await service.GetVariantsAsync(productId);
        Assert.Empty(variants.Data!);
    }

    [Fact]
    public async Task Updating_stock_to_a_negative_value_is_rejected_and_leaves_the_row_untouched()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var service = NewService(context);
        var (_, created) = await service.AddAsync(NewProduct(categoryId, "Keten Gömlek"));
        var productId = created.Data!.Id;
        await service.AddVariantAsync(new ProductVariant
        {
            ProductId = productId,
            Size = "L",
            Color = "Ekru",
            Sku = "CRV-KG-L-EKR",
            Stock = 7
        });
        var (_, variants) = await service.GetVariantsAsync(productId);
        var variantId = variants.Data![0].Id;

        var (status, result) = await service.UpdateStockAsync(variantId, -5);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.False(result.Success);
        var (_, afterUpdate) = await service.GetVariantsAsync(productId);
        Assert.Equal(7, afterUpdate.Data![0].Stock);
    }

    [Fact]
    public async Task Deleting_a_product_only_deactivates_it()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var service = NewService(context);
        var (_, created) = await service.AddAsync(NewProduct(categoryId, "Keten Gömlek"));
        var productId = created.Data!.Id;

        var (status, result) = await service.DeleteAsync(productId);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(result.Success);
        var (_, reloaded) = await service.GetByIdAsync(productId);
        Assert.NotNull(reloaded.Data);
        Assert.False(reloaded.Data!.IsActive);
    }
}
