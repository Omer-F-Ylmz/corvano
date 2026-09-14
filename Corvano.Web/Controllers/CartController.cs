using System.Net;
using Corvano.Business.Abstract;
using Corvano.Business.Utilities;
using Corvano.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Controllers;

/// <summary>
/// Sepet. Formlar JS olmadan da çalışır (POST → /sepet); fetch ile "Accept: application/json" gelirse JSON döner ve mini sepet açılır.
/// </summary>
[Route("sepet")]
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly CartOptions _options;

    public CartController(ICartService cartService, CartOptions options)
    {
        _cartService = cartService;
        _options = options;
    }

    private bool WantsJson => Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => await CartPageAsync(TempData["CartMessage"] as string, isError: false, HttpStatusCode.OK, cancellationToken);

    [HttpPost("ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int? variantId, int quantity = 1, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var back = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        if (variantId is null)
        {
            return Failed(HttpStatusCode.BadRequest, "Sepete eklemek için önce beden seçin.", back);
        }

        var (status, result) = await _cartService.AddAsync(CartCookie.Read(Request), variantId.Value, quantity, cancellationToken);
        if (!result.Success)
        {
            return Failed(status, result.Message, back);
        }

        var added = result.Data!;
        CartCookie.Write(Response, Request.IsHttps, added.CartKey, _options.CookieDays);

        if (!WantsJson)
        {
            TempData["CartMessage"] = result.Message;
            return Redirect("/sepet");
        }

        var photo = Storefront.IsPhoto(added.Line.Image) ? Storefront.Thumb(added.Line.Image!.Url) : null;
        return Json(new
        {
            ok = true,
            message = result.Message,
            itemCount = added.ItemCount,
            subtotal = Storefront.Price(added.Subtotal),
            clamped = added.WasClamped,
            line = new
            {
                name = added.Line.ProductName,
                size = added.Line.Size,
                quantity = added.Line.Quantity,
                price = Storefront.Price(added.Line.UnitPrice),
                image = photo,
                imageAlt = added.Line.Image?.Alt
            }
        });
    }

    [HttpPost("guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int quantity, CancellationToken cancellationToken)
    {
        var (status, result) = await _cartService.UpdateQuantityAsync(CartCookie.Read(Request), itemId, quantity, cancellationToken);
        if (!result.Success)
        {
            return await CartPageAsync(result.Message, isError: true, status, cancellationToken);
        }

        TempData["CartMessage"] = result.Message;
        return Redirect("/sepet");
    }

    [HttpPost("sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int itemId, CancellationToken cancellationToken)
    {
        var (status, result) = await _cartService.RemoveAsync(CartCookie.Read(Request), itemId, cancellationToken);
        if (!result.Success)
        {
            return await CartPageAsync(result.Message, isError: true, status, cancellationToken);
        }

        TempData["CartMessage"] = result.Message;
        return Redirect("/sepet");
    }

    private IActionResult Failed(HttpStatusCode status, string message, string back)
    {
        Response.StatusCode = (int)status;
        return WantsJson
            ? Json(new { ok = false, message })
            : View("AddFailed", new AddFailedViewModel(message, back));
    }

    private async Task<IActionResult> CartPageAsync(string? message, bool isError, HttpStatusCode status, CancellationToken cancellationToken)
    {
        var (_, cart) = await _cartService.GetCartAsync(CartCookie.Read(Request), cancellationToken);
        Response.StatusCode = (int)status;
        return View("Index", new CartPageViewModel(cart.Data!, _options, message, isError));
    }
}
