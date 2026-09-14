using Corvano.Business.Abstract;
using Corvano.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICatalogService _catalogService;

    public HomeController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (_, strips) = await _catalogService.GetHomeStripsAsync(cancellationToken);
        return View(new HomeViewModel(strips.Data!));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => Problem(statusCode: 500, title: "Beklenmeyen bir hata oluştu.");
}
