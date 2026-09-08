using System.Net;
using Corvano.Business.Abstract;
using Corvano.Entities.Concrete;
using Corvano.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corvano.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicy.Name)]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public CategoriesController(ICategoryService categoryService, IProductService productService)
    {
        _categoryService = categoryService;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await BuildListAsync(errorMessage: null, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
        => View("Form", new CategoryFormViewModel { SortOrder = 0, Parents = await ParentsAsync(0, cancellationToken) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Parents = await ParentsAsync(0, cancellationToken);
            return View("Form", model);
        }

        await _categoryService.AddAsync(new Category
        {
            Name = model.Name,
            ParentId = model.ParentId,
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var (status, result) = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (status != HttpStatusCode.OK)
        {
            return NotFound();
        }

        var category = result.Data!;
        return View("Form", new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            ParentId = category.ParentId,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            Parents = await ParentsAsync(category.Id, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Parents = await ParentsAsync(model.Id, cancellationToken);
            return View("Form", model);
        }

        await _categoryService.UpdateAsync(new Category
        {
            Id = model.Id,
            Name = model.Name,
            ParentId = model.ParentId,
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        }, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (status, result) = await _categoryService.DeleteAsync(id, cancellationToken);
        if (status == HttpStatusCode.OK)
        {
            return RedirectToAction(nameof(Index));
        }

        Response.StatusCode = (int)status;
        return View("Index", await BuildListAsync(result.Message, cancellationToken));
    }

    private async Task<CategoryListViewModel> BuildListAsync(string? errorMessage, CancellationToken cancellationToken)
    {
        var (_, categories) = await _categoryService.GetAllAsync(cancellationToken);
        var (_, products) = await _productService.GetAllAsync(cancellationToken);

        return new CategoryListViewModel
        {
            Categories = categories.Data!.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList(),
            ProductCounts = products.Data!
                .GroupBy(p => p.CategoryId)
                .ToDictionary(group => group.Key, group => group.Count()),
            ErrorMessage = errorMessage
        };
    }

    private async Task<List<Category>> ParentsAsync(int excludedId, CancellationToken cancellationToken)
    {
        var (_, categories) = await _categoryService.GetAllAsync(cancellationToken);
        return categories.Data!.Where(c => c.Id != excludedId).OrderBy(c => c.Name).ToList();
    }
}
