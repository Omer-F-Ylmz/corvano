using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.Web;

[Collection(DatabaseCollection.Name)]
public sealed class CartFlowTests : IAsyncLifetime
{
    private const string ProductSlug = "keten-gomlek";
    private readonly AdminWebFactory _factory = new();
    private int _variantId;

    public async Task InitializeAsync()
    {
        await TestDb.ResetAsync();
        var categoryId = await CatalogFixture.AddCategoryAsync("Gömlek", "gomlek", 1);
        var productId = await CatalogFixture.AddProductAsync(categoryId, "Keten Gömlek", ProductSlug, price: 1290m);
        _variantId = await CatalogFixture.AddVariantAsync(productId, "M", stock: 5);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static Task<HttpResponseMessage> AddToCartAsync(HttpClient client, Dictionary<string, string> fields, bool asFetch)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/sepet/ekle") { Content = null };
        return SendFormAsync(client, $"/urun/{ProductSlug}", request, fields, asFetch);
    }

    private static async Task<HttpResponseMessage> SendFormAsync(HttpClient client, string formPage, HttpRequestMessage request, Dictionary<string, string> fields, bool asFetch)
    {
        fields["__RequestVerificationToken"] = await HtmlForm.AntiforgeryTokenAsync(client, formPage);
        request.Content = new FormUrlEncodedContent(fields);
        if (asFetch)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        return await client.SendAsync(request);
    }

    private static IReadOnlyList<string> LineIds(string html)
        => Regex.Matches(html, "data-cart-line=\"(\\d+)\"").Select(m => m.Groups[1].Value).Distinct().ToList();

    [Fact]
    public async Task A_guest_without_a_cookie_gets_a_new_cart_and_keeps_adding_to_the_same_one()
    {
        var client = _factory.CreateNonRedirectingClient();

        var first = await AddToCartAsync(client, new() { ["variantId"] = _variantId.ToString(), ["quantity"] = "1" }, asFetch: true);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains(first.Headers.GetValues("Set-Cookie"), c => c.StartsWith("corvano.cart=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        await AddToCartAsync(client, new() { ["variantId"] = _variantId.ToString(), ["quantity"] = "1" }, asFetch: true);
        await AddToCartAsync(_factory.CreateNonRedirectingClient(), new() { ["variantId"] = _variantId.ToString(), ["quantity"] = "1" }, asFetch: true);

        await using var context = TestDb.NewContext();
        Assert.Equal(2, await context.Carts.CountAsync());
        Assert.Contains(await context.CartItems.ToListAsync(), i => i.Quantity == 2);
    }

    [Fact]
    public async Task Adding_without_choosing_a_size_is_a_bad_request()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await AddToCartAsync(client, new() { ["quantity"] = "1" }, asFetch: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_empty_cart_page_says_so()
    {
        var html = await _factory.CreateClient().GetStringAsync("/sepet");

        Assert.Contains("Sepetiniz boş", html);
        Assert.Empty(LineIds(html));
    }

    [Fact]
    public async Task A_shopper_adds_from_the_product_page_changes_the_quantity_sees_the_total_and_removes_the_line()
    {
        var client = _factory.CreateNonRedirectingClient();

        var added = await AddToCartAsync(client, new() { ["variantId"] = _variantId.ToString(), ["quantity"] = "1" }, asFetch: true);
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Contains("\"itemCount\":1", await added.Content.ReadAsStringAsync());

        var cart = await client.GetStringAsync("/sepet");
        var itemId = Assert.Single(LineIds(cart));

        var updated = await SendFormAsync(client, "/sepet", new HttpRequestMessage(HttpMethod.Post, "/sepet/guncelle"),
            new() { ["itemId"] = itemId, ["quantity"] = "2" }, asFetch: false);
        Assert.Equal(HttpStatusCode.Found, updated.StatusCode);

        cart = await client.GetStringAsync("/sepet");
        Assert.Contains("2.580,00 ₺", cart);   // ara toplam 2 × 1.290,00
        Assert.Contains("2.580,00 ₺", Regex.Match(cart, "data-cart-total[^>]*>([^<]+)<").Groups[1].Value); // eşik üstü: kargo yok

        var removed = await SendFormAsync(client, "/sepet", new HttpRequestMessage(HttpMethod.Post, "/sepet/sil"),
            new() { ["itemId"] = itemId }, asFetch: false);
        Assert.Equal(HttpStatusCode.Found, removed.StatusCode);
        Assert.Contains("Sepetiniz boş", await client.GetStringAsync("/sepet"));
    }

    [Fact]
    public async Task Without_javascript_the_add_form_posts_and_lands_on_the_cart_page()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await AddToCartAsync(client, new() { ["variantId"] = _variantId.ToString(), ["quantity"] = "1" }, asFetch: false);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/sepet", response.Headers.Location!.OriginalString);
        Assert.Single(LineIds(await client.GetStringAsync("/sepet")));
    }
}
