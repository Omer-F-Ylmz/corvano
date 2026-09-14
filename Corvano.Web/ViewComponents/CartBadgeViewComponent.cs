using Corvano.Business.Abstract;
using Corvano.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.ViewComponents;

/// <summary>Üst bardaki sepet bağlantısı ve adet rozeti.</summary>
public class CartBadgeViewComponent : ViewComponent
{
    private readonly ICartService _cartService;

    public CartBadgeViewComponent(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
        => View(await _cartService.GetItemCountAsync(CartCookie.Read(Request), HttpContext.RequestAborted));
}
