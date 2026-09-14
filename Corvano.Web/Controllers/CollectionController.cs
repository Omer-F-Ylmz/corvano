using System.Net;
using Corvano.Business.Abstract;
using Corvano.Entities.Dtos;
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
    public async Task<IActionResult> Index(string slug, string? alt, string? sirala, CancellationToken cancellationToken)
    {
        var sort = sirala switch
        {
            "fiyat-artan" => CollectionSort.PriceAscending,
            "fiyat-azalan" => CollectionSort.PriceDescending,
            _ => CollectionSort.Newest
        };
        var (status, result) = await _catalogService.GetCollectionAsync(slug, alt, sort, cancellationToken);
        return status == HttpStatusCode.OK ? View(result.Data!) : NotFound();
    }
}
