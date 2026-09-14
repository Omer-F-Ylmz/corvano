using System.Net;
using Corvano.Business.Abstract;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;
using Corvano.Entities.Dtos;

namespace Corvano.Business.Concrete;

public class CatalogManager : ICatalogService
{
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

    public async Task<(HttpStatusCode, IDataResult<CollectionPageDto>)> GetCollectionAsync(
        string slug,
        string? subcategorySlug,
        CollectionSort sort = CollectionSort.Newest,
        CancellationToken cancellationToken = default)
    {
        if (slug == ICatalogService.AllProductsSlug)
        {
            var all = await _productDal.GetListAsync(p => p.IsActive, cancellationToken);
            var everything = new Category { Name = "Tüm parçalar", Slug = ICatalogService.AllProductsSlug, IsActive = true };
            return (HttpStatusCode.OK, new SuccessDataResult<CollectionPageDto>(
                new CollectionPageDto(everything, [], null, sort, await CardsAsync(Sorted(all, sort), cancellationToken))));
        }

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

        var products = await _productDal.GetListAsync(p => categoryIds.Contains(p.CategoryId) && p.IsActive, cancellationToken);

        return (HttpStatusCode.OK, new SuccessDataResult<CollectionPageDto>(
            new CollectionPageDto(category, subcategories, activeSubcategory, sort, await CardsAsync(Sorted(products, sort), cancellationToken))));
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

    public async Task<(HttpStatusCode, IDataResult<List<Category>>)> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryDal.GetListAsync(c => c.ParentId == null && c.IsActive, cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<List<Category>>(categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList()));
    }

    public async Task<(HttpStatusCode, IDataResult<List<ProductCardDto>>)> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        var products = await _productDal.GetListAsync(p => p.IsActive, cancellationToken);
        var newest = Sorted(products, CollectionSort.Newest).OrderByDescending(p => p.IsFeatured).Take(count).ToList();
        return (HttpStatusCode.OK, new SuccessDataResult<List<ProductCardDto>>(await CardsAsync(newest, cancellationToken)));
    }

    public async Task<(HttpStatusCode, IDataResult<List<ProductCardDto>>)> GetSimilarAsync(string slug, int count, CancellationToken cancellationToken = default)
    {
        var product = await _productDal.GetAsync(p => p.Slug == slug, cancellationToken);
        if (product is null)
        {
            return (HttpStatusCode.NotFound, new ErrorDataResult<List<ProductCardDto>>("Ürün bulunamadı."));
        }

        var siblings = await _productDal.GetListAsync(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive, cancellationToken);
        var similar = Sorted(siblings, CollectionSort.Newest).Take(count).ToList();
        return (HttpStatusCode.OK, new SuccessDataResult<List<ProductCardDto>>(await CardsAsync(similar, cancellationToken)));
    }

    public async Task<(HttpStatusCode, IDataResult<List<HomeCategoryDto>>)> GetHomeCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryDal.GetListAsync(c => c.IsActive, cancellationToken);
        var products = Sorted(await _productDal.GetListAsync(p => p.IsActive, cancellationToken), CollectionSort.Newest);
        var cards = await CardsAsync(products, cancellationToken);

        var result = categories
            .Where(c => c.ParentId is null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(top =>
            {
                var inTree = cards.Where(card => card.CategorySlug == top.Slug).ToList();
                return new HomeCategoryDto(top, inTree.FirstOrDefault(card => card.Images.Count > 0) ?? inTree.FirstOrDefault(), inTree.Count);
            })
            .ToList();

        return (HttpStatusCode.OK, new SuccessDataResult<List<HomeCategoryDto>>(result));
    }

    private static List<Product> Sorted(IEnumerable<Product> products, CollectionSort sort) => sort switch
    {
        CollectionSort.PriceAscending => products.OrderBy(p => p.Price).ThenBy(p => p.Name).ToList(),
        CollectionSort.PriceDescending => products.OrderByDescending(p => p.Price).ThenBy(p => p.Name).ToList(),
        _ => products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name).ToList()
    };

    private async Task<List<ProductCardDto>> CardsAsync(List<Product> products, CancellationToken cancellationToken)
    {
        var ids = products.Select(p => p.Id).ToList();
        var images = await _imageDal.GetListAsync(i => ids.Contains(i.ProductId), cancellationToken);
        var variants = await _variantDal.GetListAsync(v => ids.Contains(v.ProductId), cancellationToken);
        var categories = await _categoryDal.GetListAsync(cancellationToken: cancellationToken);

        string TopSlug(int categoryId)
        {
            var category = categories.FirstOrDefault(c => c.Id == categoryId);
            var parent = category?.ParentId is { } parentId ? categories.FirstOrDefault(c => c.Id == parentId) : null;
            return (parent ?? category)?.Slug ?? string.Empty;
        }

        return products.Select(p => new ProductCardDto(
            p,
            TopSlug(p.CategoryId),
            images.Where(i => i.ProductId == p.Id).OrderBy(i => i.SortOrder).ToList(),
            variants.Where(v => v.ProductId == p.Id).OrderBy(v => v.Id).ToList())).ToList();
    }
}
