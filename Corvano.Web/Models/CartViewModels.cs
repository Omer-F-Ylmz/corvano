using Corvano.Business.Utilities;
using Corvano.Entities.Dtos;

namespace Corvano.Web.Models;

public sealed record CartPageViewModel(CartDto Cart, CartOptions Options, string? Message, bool IsError);

public sealed record AddFailedViewModel(string Message, string ReturnUrl);

/// <summary>Misafir sepeti çerezi: cart_key GUID, HttpOnly, SameSite=Lax.</summary>
public static class CartCookie
{
    public const string Name = "corvano.cart";

    public static Guid? Read(HttpRequest request)
        => Guid.TryParse(request.Cookies[Name], out var key) ? key : null;

    public static void Write(HttpResponse response, bool isHttps, Guid key, int days)
        => response.Cookies.Append(Name, key.ToString(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = isHttps,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(days)
        });
}
