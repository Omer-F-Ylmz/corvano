using System.Net;
using Corvano.Business.Abstract;
using Corvano.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Controllers;

public class ProductController : Controller
{
    private readonly ICatalogService _catalogService;

    public ProductController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("urun/{slug}")]
    public async Task<IActionResult> Detail(string slug, int? boy, int? kilo, CancellationToken cancellationToken)
    {
        var (status, result) = await _catalogService.GetProductAsync(slug, cancellationToken);
        return status == HttpStatusCode.OK
            ? View(new ProductViewModel(result.Data!, boy, kilo))
            : NotFound();
    }
}
