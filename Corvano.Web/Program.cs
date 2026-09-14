using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Corvano.Business.Abstract;
using Corvano.Business.DependencyResolvers.Autofac;
using Corvano.Business.Utilities;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Web.ModelBinding;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container => container.RegisterModule(new AutofacBusinessModule()));

builder.Services.AddControllersWithViews(options =>
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider()));

// Türkçe harfler, ₺ ve tire/nokta gibi işaretler HTML kaynağında entity'ye çevrilmesin.
builder.Services.AddSingleton(HtmlEncoder.Create(
    UnicodeRanges.BasicLatin,
    UnicodeRanges.Latin1Supplement,
    UnicodeRanges.LatinExtendedA,
    UnicodeRanges.GeneralPunctuation,
    UnicodeRanges.CurrencySymbols));

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

builder.Services.AddSingleton(builder.Configuration.GetSection("Cart").Get<CartOptions>() ?? new CartOptions());

builder.Services.AddDbContext<CorvanoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddHealthChecks();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "corvano.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/admin/auth/login";
        options.LogoutPath = "/admin/auth/logout";
        options.AccessDeniedPath = "/admin/auth/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminPolicy.Name, policy => policy.RequireClaim(AdminPolicy.ClaimType, AdminPolicy.ClaimValue));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Görünüm tr-TR ("1.290,00 ₺"); decimal girdiler InvariantDecimalModelBinder ile kültürden bağımsız bağlanır.
var turkish = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkish),
    SupportedCultures = [turkish],
    SupportedUICultures = [turkish]
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/admin", () => Results.Redirect("/admin/products"));
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await SeedFirstAdminAsync(app);
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Catalog:SeedDemo", true))
{
    await SeedCatalogAsync(app);
}

app.Run();

static async Task SeedFirstAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var email = app.Configuration["Admin:Email"];
    var password = app.Configuration["Admin:Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogCritical(
            "Admin:Email / Admin:Password tanımsız. İlk yönetici oluşturulmadı; /admin'e giriş yapılamaz.");
        return;
    }

    var authService = scope.ServiceProvider.GetRequiredService<IAdminAuthService>();
    var (_, result) = await authService.EnsureSeedAsync(email, password);
    logger.LogInformation("Yönetici tohumlama: {Message}", result.Message);
}

static async Task SeedCatalogAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var (_, result) = await scope.ServiceProvider.GetRequiredService<ICatalogSeedService>().EnsureSeedAsync();
    scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogInformation("Katalog tohumlama: {Message}", result.Message);
}

/// <summary>Yalnız yönetici çerezine sahip isteklerin /admin altına girmesini sağlar.</summary>
public static class AdminPolicy
{
    public const string Name = "Admin";
    public const string ClaimType = "corvano:admin";
    public const string ClaimValue = "true";
}

public partial class Program
{
}
