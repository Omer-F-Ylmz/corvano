using System.Net;
using Corvano.Business.Concrete;
using Corvano.Business.Utilities;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class CartManagerTests : IAsyncLifetime
{
    private int _categoryId;

    public async Task InitializeAsync()
    {
        await TestDb.ResetAsync();
        _categoryId = await CatalogFixture.AddCategoryAsync("Gömlek", "gomlek", 1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CartManager NewService(CorvanoContext context) => new(
        new EfCartDal(context),
        new EfCartItemDal(context),
        new EfProductVariantDal(context),
        new EfProductDal(context),
        new EfCategoryDal(context),
        new EfProductImageDal(context),
        new EfUnitOfWork(context),
        new CartOptions { ShippingFee = 149.90m, FreeShippingThreshold = 2500m });

    private async Task<(int ProductId, int VariantId)> VariantAsync(string slug, decimal price = 1290m, int stock = 5)
    {
        var productId = await CatalogFixture.AddProductAsync(_categoryId, slug, slug, price: price);
        return (productId, await CatalogFixture.AddVariantAsync(productId, "M", stock));
    }

    [Fact]
    public async Task Adding_to_a_new_cart_creates_the_cart_and_a_line_priced_at_that_moment()
    {
        var (_, variantId) = await VariantAsync("keten-gomlek");
        await using var context = TestDb.NewContext();

        var (status, result) = await NewService(context).AddAsync(null, variantId, 1);

        Assert.Equal(HttpStatusCode.Created, status);
        Assert.NotEqual(Guid.Empty, result.Data!.CartKey);
        Assert.Equal(1, result.Data.Line.Quantity);
        Assert.Equal(1290m, result.Data.Line.UnitPrice);
        Assert.Equal(1, result.Data.ItemCount);
    }

    [Fact]
    public async Task Adding_the_same_variant_again_sums_the_quantity_on_one_line()
    {
        var (_, variantId) = await VariantAsync("keten-gomlek");
        await using var context = TestDb.NewContext();
        var service = NewService(context);

        var (_, first) = await service.AddAsync(null, variantId, 1);
        var (status, second) = await service.AddAsync(first.Data!.CartKey, variantId, 2);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(3, second.Data!.Line.Quantity);
        Assert.Single(await context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task Adding_beyond_stock_is_clamped_to_the_stock_with_a_warning()
    {
        var (_, variantId) = await VariantAsync("keten-gomlek", stock: 5);
        await using var context = TestDb.NewContext();
        var service = NewService(context);

        var (_, first) = await service.AddAsync(null, variantId, 3);
        var (status, second) = await service.AddAsync(first.Data!.CartKey, variantId, 4);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(second.Data!.WasClamped);
        Assert.Equal(5, second.Data.Line.Quantity);
        Assert.Contains("5", second.Message);
    }

    [Fact]
    public async Task Setting_the_quantity_to_zero_removes_the_line()
    {
        var (_, variantId) = await VariantAsync("keten-gomlek");
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        var (_, added) = await service.AddAsync(null, variantId, 2);

        var (status, _) = await service.UpdateQuantityAsync(added.Data!.CartKey, added.Data.Line.ItemId, 0);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(await context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task Setting_the_quantity_above_stock_is_a_conflict_and_keeps_the_line()
    {
        var (_, variantId) = await VariantAsync("keten-gomlek", stock: 4);
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        var (_, added) = await service.AddAsync(null, variantId, 2);

        var (status, result) = await service.UpdateQuantityAsync(added.Data!.CartKey, added.Data.Line.ItemId, 9);

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Contains("4", result.Message);
        Assert.Equal(2, (await context.CartItems.AsNoTracking().SingleAsync()).Quantity);
    }

    [Fact]
    public async Task A_line_of_a_deactivated_product_stays_marked_but_is_left_out_of_the_totals()
    {
        var (soldOutProduct, soldOutVariant) = await VariantAsync("eski-gomlek", price: 900m);
        var (_, variantId) = await VariantAsync("keten-gomlek", price: 1290m);
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        var (_, added) = await service.AddAsync(null, soldOutVariant, 1);
        await service.AddAsync(added.Data!.CartKey, variantId, 1);
        await context.Products.Where(p => p.Id == soldOutProduct).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        var (_, cart) = await NewService(TestDb.NewContext()).GetCartAsync(added.Data.CartKey);

        Assert.Equal(2, cart.Data!.Lines.Count);
        var line = Assert.Single(cart.Data.Lines, l => !l.IsAvailable);
        Assert.Contains("artık satışta değil", line.Warning);
        Assert.Equal(1290m, cart.Data.Subtotal);
    }

    [Theory]
    [InlineData(2490, 149.90, 2639.90)]
    [InlineData(2600, 0, 2600)]
    public async Task Shipping_is_charged_below_the_threshold_and_free_above_it(decimal price, decimal shipping, decimal total)
    {
        var (_, variantId) = await VariantAsync("palto", price: price);
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        var (_, added) = await service.AddAsync(null, variantId, 1);

        var (_, cart) = await service.GetCartAsync(added.Data!.CartKey);

        Assert.Equal(price, cart.Data!.Subtotal);
        Assert.Equal(shipping, cart.Data.Shipping);
        Assert.Equal(total, cart.Data.Total);
    }

    [Fact]
    public async Task A_changed_price_updates_the_line_and_flags_it()
    {
        var (productId, variantId) = await VariantAsync("keten-gomlek", price: 1290m);
        await using var context = TestDb.NewContext();
        var (_, added) = await NewService(context).AddAsync(null, variantId, 1);
        await context.Products.Where(p => p.Id == productId).ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, 1390m));

        await using var reader = TestDb.NewContext();
        var (_, cart) = await NewService(reader).GetCartAsync(added.Data!.CartKey);

        var line = Assert.Single(cart.Data!.Lines);
        Assert.True(line.PriceUpdated);
        Assert.Equal(1390m, line.UnitPrice);
        Assert.Equal(1390m, (await reader.CartItems.AsNoTracking().SingleAsync()).UnitPrice);
    }

    [Fact]
    public async Task Two_simultaneous_adds_of_the_same_variant_end_in_one_line()
    {
        var (_, variantId) = await VariantAsync("keten-gomlek", stock: 10);
        var (_, otherVariant) = await VariantAsync("oxford-gomlek", stock: 10);
        Guid cartKey;
        await using (var context = TestDb.NewContext())
        {
            var (_, created) = await NewService(context).AddAsync(null, otherVariant, 1);
            cartKey = created.Data!.CartKey;
        }

        async Task AddOnceAsync()
        {
            await using var context = TestDb.NewContext();
            await NewService(context).AddAsync(cartKey, variantId, 1);
        }

        await Task.WhenAll(AddOnceAsync(), AddOnceAsync());

        await using var check = TestDb.NewContext();
        var line = Assert.Single(await check.CartItems.Where(i => i.ProductVariantId == variantId).ToListAsync());
        Assert.Equal(2, line.Quantity);
    }
}
