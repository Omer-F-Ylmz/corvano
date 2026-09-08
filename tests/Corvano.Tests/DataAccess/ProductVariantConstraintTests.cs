using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.DataAccess;

[Collection(DatabaseCollection.Name)]
public sealed class ProductVariantConstraintTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<int> SeedProductAsync(CorvanoContext context)
    {
        var category = new Category { Name = "Gömlek", Slug = "gomlek", SortOrder = 1, IsActive = true };
        await new EfCategoryDal(context).AddAsync(category);
        await new EfUnitOfWork(context).SaveChangesAsync();

        var product = new Product
        {
            Name = "Keten Gömlek",
            Slug = "keten-gomlek",
            Description = "Pamuk karışımı.",
            CategoryId = category.Id,
            Price = 1290m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await new EfProductDal(context).AddAsync(product);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return product.Id;
    }

    private static ProductVariant NewVariant(int productId, string sku, int stock) => new()
    {
        ProductId = productId,
        Size = "M",
        Color = "Ekru",
        Sku = sku,
        Stock = stock
    };

    [Fact]
    public async Task Adding_a_second_variant_with_the_same_sku_is_rejected_by_the_unique_index()
    {
        await using var context = TestDb.NewContext();
        var productId = await SeedProductAsync(context);
        var dal = new EfProductVariantDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        await dal.AddAsync(NewVariant(productId, "CRV-KG-M-EKR", 5));
        await unitOfWork.SaveChangesAsync();

        await dal.AddAsync(NewVariant(productId, "CRV-KG-M-EKR", 3));

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task Negative_stock_is_rejected_by_the_check_constraint()
    {
        await using var context = TestDb.NewContext();
        var productId = await SeedProductAsync(context);
        var dal = new EfProductVariantDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        await dal.AddAsync(NewVariant(productId, "CRV-KG-S-EKR", -1));

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }
}
