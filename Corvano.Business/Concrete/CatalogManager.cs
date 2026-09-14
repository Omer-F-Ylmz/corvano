using System.Net;
using Corvano.Business.Abstract;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;
using Corvano.Entities.Dtos;

namespace Corvano.Business.Concrete;

public class CatalogManager : ICatalogService
{
    private const int HomeStripCount = 3;

    private readonly ICategoryDal _categoryDal;
    private readonly IProductDal _productDal;
    private readonly IProductVariantDal _variantDal;
    private readonly IProductImageDal _imageDal;

    public CatalogManager(ICategoryDal categoryDal, IProductDal productDal, IProductVariantDal variantDal, IProductImageDal imageDal)
    {
        _categoryDal = categoryDal;
        _productDal = productDal;
        _variantDal = variantDal;
        _imageDal = imageDal;
    }

    public async Task<(HttpStatusCode, IDataResult<CollectionPageDto>)> GetCollectionAsync(string slug, string? subcategorySlug, CancellationToken cancellationToken = default)
    {
        var category = await _categoryDal.GetAsync(c => c.Slug == slug && c.ParentId == null && c.IsActive, cancellationToken);
        if (category is null)
        {
            return (HttpStatusCode.NotFound, new ErrorDataResult<CollectionPageDto>("Koleksiyon bulunamadı."));
        }

        var subcategories = (await _categoryDal.GetListAsync(c => c.ParentId == category.Id && c.IsActive, cancellationToken))
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
        var activeSubcategory = subcategories.FirstOrDefault(c => c.Slug == subcategorySlug);

        List<int> categoryIds = activeSubcategory is not null
            ? [activeSubcategory.Id]
            : subcategories.Select(c => c.Id).Append(category.Id).ToList();

        var products = (await _productDal.GetListAsync(p => categoryIds.Contains(p.CategoryId) && p.IsActive, cancellationToken))
            .OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name).ToList();

        return (HttpStatusCode.OK, new SuccessDataResult<CollectionPageDto>(
            new CollectionPageDto(category, subcategories, activeSubcategory, await CardsAsync(products, cancellationToken))));
    }

    public async Task<(HttpStatusCode, IDataResult<ProductPageDto>)> GetProductAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _productDal.GetAsync(p => p.Slug == slug && p.IsActive, cancellationToken);
        var category = product is null
            ? null
            : await _categoryDal.GetAsync(c => c.Id == product.CategoryId && c.IsActive, cancellationToken);
        if (product is null || category is null)
        {
            return (HttpStatusCode.NotFound, new ErrorDataResult<ProductPageDto>("Ürün bulunamadı."));
        }

        var parent = category.ParentId is { } parentId
            ? await _categoryDal.GetAsync(c => c.Id == parentId, cancellationToken)
            : null;
        var images = await _imageDal.GetListAsync(i => i.ProductId == product.Id, cancellationToken);
        var variants = await _variantDal.GetListAsync(v => v.ProductId == product.Id, cancellationToken);

        return (HttpStatusCode.OK, new SuccessDataResult<ProductPageDto>(new ProductPageDto(
            product,
            category,
            parent,
            images.OrderBy(i => i.SortOrder).ToList(),
            variants.OrderBy(v => v.Id).ToList())));
    }

    public async Task<(HttpStatusCode, IDataResult<List<HomeStripDto>>)> GetHomeStripsAsync(CancellationToken cancellationToken = default)
    {
        var categories = (await _categoryDal.GetListAsync(c => c.ParentId == null && c.IsActive, cancellationToken))
            .OrderBy(c => c.SortOrder).Take(HomeStripCount).ToList();

        var strips = new List<HomeStripDto>();
        foreach (var category in categories)
        {
            var productIds = (await _productDal.GetListAsync(p => p.CategoryId == category.Id && p.IsActive, cancellationToken))
                .Select(p => p.Id).ToList();
            var images = (await _imageDal.GetListAsync(i => productIds.Contains(i.ProductId), cancellationToken))
                .OrderBy(i => i.SortOrder).ThenBy(i => i.ProductId).Take(2).ToList();
            strips.Add(new HomeStripDto(category, images));
        }

        return (HttpStatusCode.OK, new SuccessDataResult<List<HomeStripDto>>(strips));
    }

    private async Task<List<ProductCardDto>> CardsAsync(List<Product> products, CancellationToken cancellationToken)
    {
        var ids = products.Select(p => p.Id).ToList();
        var images = await _imageDal.GetListAsync(i => ids.Contains(i.ProductId), cancellationToken);
        var variants = await _variantDal.GetListAsync(v => ids.Contains(v.ProductId), cancellationToken);

        return products.Select(p => new ProductCardDto(
            p,
            images.Where(i => i.ProductId == p.Id).OrderBy(i => i.SortOrder).ToList(),
            variants.Where(v => v.ProductId == p.Id).OrderBy(v => v.Id).ToList())).ToList();
    }
}
