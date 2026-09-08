using System.Net;
using Corvano.Business.Abstract;
using Corvano.Entities.Concrete;
using Corvano.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicy.Name)]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductsController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (_, products) = await _productService.GetAllAsync(cancellationToken);
        var (_, categories) = await _categoryService.GetAllAsync(cancellationToken);

        var stockTotals = new Dictionary<int, int>();
        foreach (var product in products.Data!)
        {
            var (_, variants) = await _productService.GetVariantsAsync(product.Id, cancellationToken);
            stockTotals[product.Id] = variants.Data!.Sum(v => v.Stock);
        }

        return View(new ProductListViewModel
        {
            Products = products.Data!.OrderByDescending(p => p.UpdatedAt).ToList(),
            CategoryNames = categories.Data!.ToDictionary(c => c.Id, c => c.Name),
            StockTotals = stockTotals
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
        => View(new ProductFormViewModel { Categories = await CategoriesAsync(cancellationToken) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await CategoriesAsync(cancellationToken);
            return View(model);
        }

        var (_, created) = await _productService.AddAsync(new Product
        {
            Name = model.Name,
            Description = model.Description,
            CategoryId = model.CategoryId,
            Price = model.Price,
            IsActive = model.IsActive
        }, cancellationToken);

        return RedirectToAction(nameof(Edit), new { id = created.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await BuildFormAsync(id, errorMessage: null, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var reloaded = await BuildFormAsync(model.Id, errorMessage: null, cancellationToken);
            model.Categories = reloaded?.Categories ?? [];
            model.Variants = reloaded?.Variants ?? [];
            model.Images = reloaded?.Images ?? [];
            return View(model);
        }

        await _productService.UpdateAsync(new Product
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            CategoryId = model.CategoryId,
            Price = model.Price,
            IsActive = model.IsActive
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(VariantFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormWithErrorAsync(model.ProductId, "Varyant alanlarını kontrol edin.", cancellationToken);
        }

        var (status, result) = await _productService.AddVariantAsync(new ProductVariant
        {
            ProductId = model.ProductId,
            Size = model.Size,
            Color = model.Color,
            Sku = model.Sku,
            Stock = model.Stock,
            PriceOverride = model.PriceOverride
        }, cancellationToken);

        return status == HttpStatusCode.Created
            ? RedirectToAction(nameof(Edit), new { id = model.ProductId })
            : await FormWithErrorAsync(model.ProductId, result.Message, cancellationToken, status);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int productId, int variantId, int stock, CancellationToken cancellationToken)
    {
        var (status, result) = await _productService.UpdateStockAsync(variantId, stock, cancellationToken);

        return status == HttpStatusCode.OK
            ? RedirectToAction(nameof(Edit), new { id = productId })
            : await FormWithErrorAsync(productId, result.Message, cancellationToken, status);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(int productId, int variantId, CancellationToken cancellationToken)
    {
        await _productService.DeleteVariantAsync(variantId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImage(ImageFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormWithErrorAsync(model.ProductId, "Görsel alanlarını kontrol edin.", cancellationToken);
        }

        await _productService.AddImageAsync(new ProductImage
        {
            ProductId = model.ProductId,
            Url = model.Url,
            Alt = model.Alt,
            SortOrder = model.SortOrder
        }, cancellationToken);

        return RedirectToAction(nameof(Edit), new { id = model.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int productId, int imageId, CancellationToken cancellationToken)
    {
        await _productService.DeleteImageAsync(imageId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    private async Task<IActionResult> FormWithErrorAsync(
        int productId,
        string message,
        CancellationToken cancellationToken,
        HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var model = await BuildFormAsync(productId, message, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        Response.StatusCode = (int)status;
        return View("Edit", model);
    }

    private async Task<ProductFormViewModel?> BuildFormAsync(int id, string? errorMessage, CancellationToken cancellationToken)
    {
        var (status, stored) = await _productService.GetByIdAsync(id, cancellationToken);
        if (status != HttpStatusCode.OK)
        {
            return null;
        }

        var product = stored.Data!;
        var (_, variants) = await _productService.GetVariantsAsync(id, cancellationToken);
        var (_, images) = await _productService.GetImagesAsync(id, cancellationToken);

        return new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId,
            Price = product.Price,
            IsActive = product.IsActive,
            Slug = product.Slug,
            Categories = await CategoriesAsync(cancellationToken),
            Variants = variants.Data!.OrderBy(v => v.Size).ThenBy(v => v.Color).ToList(),
            Images = images.Data!.OrderBy(i => i.SortOrder).ToList(),
            ErrorMessage = errorMessage
        };
    }

    private async Task<List<Category>> CategoriesAsync(CancellationToken cancellationToken)
    {
        var (_, categories) = await _categoryService.GetAllAsync(cancellationToken);
        return categories.Data!.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
    }
}
