using System.Net;
using Corvano.DataAccess.Concrete.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.Web;

[Collection(DatabaseCollection.Name)]
public sealed class AdminCrudFlowTests : IAsyncLifetime
{
    private readonly AdminWebFactory _factory = new();

    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task SignInAsync(HttpClient client)
    {
        var response = await HtmlForm.PostAsync(client, "/admin/auth/login", "/admin/auth/login",
            new Dictionary<string, string>
            {
                ["Email"] = AdminWebFactory.AdminEmail,
                ["Password"] = AdminWebFactory.AdminPassword
            });

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/admin/products", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Signing_in_with_a_wrong_password_keeps_the_visitor_on_the_login_page()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await HtmlForm.PostAsync(client, "/admin/auth/login", "/admin/auth/login",
            new Dictionary<string, string>
            {
                ["Email"] = AdminWebFactory.AdminEmail,
                ["Password"] = "yanlis-parola"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("E-posta ya da parola hatalı", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_admin_can_sign_in_then_create_a_category_a_product_a_variant_and_update_its_stock()
    {
        var client = _factory.CreateNonRedirectingClient();
        await SignInAsync(client);

        var categoryResponse = await HtmlForm.PostAsync(client, "/admin/categories/create", "/admin/categories/create",
            new Dictionary<string, string>
            {
                ["Name"] = "Gömlek",
                ["SortOrder"] = "1",
                ["IsActive"] = "true"
            });
        Assert.Equal(HttpStatusCode.Found, categoryResponse.StatusCode);

        await using var context = TestDb.NewContext();
        var category = Assert.Single(await new EfCategoryDal(context).GetListAsync());
        Assert.Equal("gomlek", category.Slug);

        var productResponse = await HtmlForm.PostAsync(client, "/admin/products/create", "/admin/products/create",
            new Dictionary<string, string>
            {
                ["Name"] = "Keten Gömlek",
                ["Description"] = "Yıkanmış keten, düşük omuz.",
                ["CategoryId"] = category.Id.ToString(),
                ["Price"] = "1290",
                ["IsActive"] = "true"
            });
        Assert.Equal(HttpStatusCode.Found, productResponse.StatusCode);

        var editUrl = productResponse.Headers.Location!.OriginalString;
        var productId = int.Parse(editUrl.Split('/')[^1]);

        var variantResponse = await HtmlForm.PostAsync(client, editUrl, "/admin/products/addvariant",
            new Dictionary<string, string>
            {
                ["ProductId"] = productId.ToString(),
                ["Size"] = "M",
                ["Color"] = "Ekru",
                ["Sku"] = "CRV-KG-M-EKR",
                ["Stock"] = "4"
            });
        Assert.Equal(HttpStatusCode.Found, variantResponse.StatusCode);

        var variant = Assert.Single(await new EfProductVariantDal(context).GetListAsync(v => v.ProductId == productId));

        var stockResponse = await HtmlForm.PostAsync(client, editUrl, "/admin/products/updatestock",
            new Dictionary<string, string>
            {
                ["ProductId"] = productId.ToString(),
                ["VariantId"] = variant.Id.ToString(),
                ["Stock"] = "12"
            });
        Assert.Equal(HttpStatusCode.Found, stockResponse.StatusCode);

        var updated = await new EfProductVariantDal(context).GetAsync(v => v.Id == variant.Id);
        Assert.Equal(12, updated!.Stock);

        var list = await client.GetAsync("/admin/products");
        Assert.Contains("Keten Gömlek", await list.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Deleting_a_category_that_has_products_reports_a_conflict()
    {
        var client = _factory.CreateNonRedirectingClient();
        await SignInAsync(client);

        await HtmlForm.PostAsync(client, "/admin/categories/create", "/admin/categories/create",
            new Dictionary<string, string> { ["Name"] = "Pantolon", ["SortOrder"] = "1", ["IsActive"] = "true" });

        await using var context = TestDb.NewContext();
        var category = Assert.Single(await new EfCategoryDal(context).GetListAsync());

        await HtmlForm.PostAsync(client, "/admin/products/create", "/admin/products/create",
            new Dictionary<string, string>
            {
                ["Name"] = "Yün Pantolon",
                ["Description"] = "Kışlık.",
                ["CategoryId"] = category.Id.ToString(),
                ["Price"] = "2490",
                ["IsActive"] = "true"
            });

        var response = await HtmlForm.PostAsync(client, "/admin/categories", "/admin/categories/delete",
            new Dictionary<string, string> { ["Id"] = category.Id.ToString() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(await new EfCategoryDal(context).GetListAsync());
    }
}
