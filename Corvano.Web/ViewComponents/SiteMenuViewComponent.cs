using Corvano.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.ViewComponents;

/// <summary>Üst bar kategori menüsü: yayındaki üst kategoriler, sort_order sırasıyla.</summary>
public class SiteMenuViewComponent : ViewComponent
{
    private readonly ICatalogService _catalogService;

    public SiteMenuViewComponent(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var (_, menu) = await _catalogService.GetMenuAsync(HttpContext.RequestAborted);
        return View(menu.Data!);
    }
}
