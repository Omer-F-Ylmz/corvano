using System.Net;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.Entities.Concrete;

namespace Corvano.Tests.Web;

[Collection(DatabaseCollection.Name)]
public sealed class CultureAndRoutingTests : IAsyncLifetime
{
    private readonly AdminWebFactory _factory = new();

    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<int> SeedCategoryAsync()
    {
        await using var context = TestDb.NewContext();
        var category = new Category { Name = "Gömlek", Slug = "gomlek", SortOrder = 1, IsActive = true };
        await new EfCategoryDal(context).AddAsync(category);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return category.Id;
    }

    [Theory]
    [InlineData("1290,00")]
    [InlineData("1290.00")]
    public async Task A_price_typed_with_a_comma_or_a_dot_binds_to_the_same_decimal(string typedPrice)
    {
        var categoryId = await SeedCategoryAsync();
        var client = _factory.CreateNonRedirectingClient();
        await HtmlForm.SignInAsAdminAsync(client);

        var response = await HtmlForm.PostAsync(client, "/admin/products/create", "/admin/products/create",
            new Dictionary<string, string>
            {
                ["Name"] = "Keten Gömlek",
                ["Description"] = "Keten | Türkiye | 30°C",
                ["CategoryId"] = categoryId.ToString(),
                ["Price"] = typedPrice,
                ["IsActive"] = "true"
            });

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        await using var context = TestDb.NewContext();
        var product = Assert.Single(await new EfProductDal(context).GetListAsync());
        Assert.Equal(1290m, product.Price);
    }

    [Fact]
    public async Task The_admin_price_input_is_written_with_an_invariant_decimal_point()
    {
        var categoryId = await SeedCategoryAsync();
        await using var context = TestDb.NewContext();
        var product = new Product
        {
            Name = "Keten Gömlek",
            Slug = "keten-gomlek",
            Description = "Keten | Türkiye | 30°C",
            CategoryId = categoryId,
            Price = 1290.5m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await new EfProductDal(context).AddAsync(product);
        await new EfUnitOfWork(context).SaveChangesAsync();
        var client = _factory.CreateNonRedirectingClient();
        await HtmlForm.SignInAsAdminAsync(client);

        var html = await (await client.GetAsync($"/admin/products/edit/{product.Id}")).Content.ReadAsStringAsync();

        Assert.Contains("value=\"1290.50\"", html);
    }

    [Fact]
    public async Task The_admin_root_redirects_to_the_product_list()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/products", response.Headers.Location!.OriginalString);
    }
}
