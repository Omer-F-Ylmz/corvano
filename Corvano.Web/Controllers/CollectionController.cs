using System.Net;
using Corvano.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Controllers;

public class CollectionController : Controller
{
    private readonly ICatalogService _catalogService;

    public CollectionController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("koleksiyon/{slug}")]
    public async Task<IActionResult> Index(string slug, string? alt, CancellationToken cancellationToken)
    {
        var (status, result) = await _catalogService.GetCollectionAsync(slug, alt, cancellationToken);
        return status == HttpStatusCode.OK ? View(result.Data!) : NotFound();
    }
}
