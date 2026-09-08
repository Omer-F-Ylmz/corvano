using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Corvano.Tests.Web;

public sealed class AdminWebFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@corvano.com";
    public const string AdminPassword = "Corvano!Test1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestDb.ConnectionString,
                ["Admin:Email"] = AdminEmail,
                ["Admin:Password"] = AdminPassword
            }));
    }

    public HttpClient CreateNonRedirectingClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
}

public static class HtmlForm
{
    private static readonly Regex TokenPattern = new(
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
        RegexOptions.IgnoreCase);

    public static async Task<string> AntiforgeryTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenPattern.Match(html);
        Assert.True(match.Success, $"{url} sayfasında antiforgery alanı bulunamadı.");
        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string formUrl,
        string postUrl,
        Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await AntiforgeryTokenAsync(client, formUrl);
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
    }
}
