using System.Net;
using Corvano.Business.Abstract;
using Corvano.Business.Utilities;
using Corvano.Core.DataAccess;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly IProductVariantDal _variantDal;
    private readonly IProductImageDal _imageDal;
    private readonly IUnitOfWork _unitOfWork;

    public ProductManager(
        IProductDal productDal,
        IProductVariantDal variantDal,
        IProductImageDal imageDal,
        IUnitOfWork unitOfWork)
    {
        _productDal = productDal;
        _variantDal = variantDal;
        _imageDal = imageDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<(HttpStatusCode, IDataResult<List<Product>>)> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productDal.GetListAsync(cancellationToken: cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<List<Product>>(products));
    }

    public async Task<(HttpStatusCode, IDataResult<Product>)> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productDal.GetAsync(p => p.Id == id, cancellationToken);
        return product is null
            ? (HttpStatusCode.NotFound, new ErrorDataResult<Product>("Ürün bulunamadı."))
            : (HttpStatusCode.OK, new SuccessDataResult<Product>(product));
    }

    public async Task<(HttpStatusCode, IDataResult<Product>)> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.Slug = await UniqueSlugAsync(product.Name, excludedId: 0, cancellationToken);
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = product.CreatedAt;

        await _productDal.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.Created, new SuccessDataResult<Product>(product, "Ürün eklendi."));
    }

    public async Task<(HttpStatusCode, IResult)> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var stored = await _productDal.GetAsync(p => p.Id == product.Id, cancellationToken);
        if (stored is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Ürün bulunamadı."));
        }

        stored.Name = product.Name;
        stored.Description = product.Description;
        stored.CategoryId = product.CategoryId;
        stored.Price = product.Price;
        stored.IsActive = product.IsActive;
        stored.Slug = await UniqueSlugAsync(product.Name, stored.Id, cancellationToken);
        stored.UpdatedAt = DateTime.UtcNow;

        _productDal.Update(stored);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Ürün güncellendi."));
    }

    public async Task<(HttpStatusCode, IResult)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productDal.GetAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Ürün bulunamadı."));
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        _productDal.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Ürün pasife alındı."));
    }

    public async Task<(HttpStatusCode, IDataResult<List<ProductVariant>>)> GetVariantsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var variants = await _variantDal.GetListAsync(v => v.ProductId == productId, cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<List<ProductVariant>>(variants));
    }

    public async Task<(HttpStatusCode, IResult)> AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        if (variant.Stock < 0)
        {
            return (HttpStatusCode.BadRequest, new ErrorResult("Stok negatif olamaz."));
        }

        if (await _variantDal.GetAsync(v => v.Sku == variant.Sku, cancellationToken) is not null)
        {
            return (HttpStatusCode.Conflict, new ErrorResult($"'{variant.Sku}' stok kodu başka bir varyantta kullanılıyor."));
        }

        await _variantDal.AddAsync(variant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.Created, new SuccessResult("Varyant eklendi."));
    }

    public async Task<(HttpStatusCode, IResult)> UpdateStockAsync(int variantId, int stock, CancellationToken cancellationToken = default)
    {
        if (stock < 0)
        {
            return (HttpStatusCode.BadRequest, new ErrorResult("Stok negatif olamaz."));
        }

        var variant = await _variantDal.GetAsync(v => v.Id == variantId, cancellationToken);
        if (variant is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Varyant bulunamadı."));
        }

        variant.Stock = stock;
        _variantDal.Update(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Stok güncellendi."));
    }

    public async Task<(HttpStatusCode, IResult)> DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default)
    {
        var variant = await _variantDal.GetAsync(v => v.Id == variantId, cancellationToken);
        if (variant is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Varyant bulunamadı."));
        }

        _variantDal.Delete(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Varyant silindi."));
    }

    public async Task<(HttpStatusCode, IDataResult<List<ProductImage>>)> GetImagesAsync(int productId, CancellationToken cancellationToken = default)
    {
        var images = await _imageDal.GetListAsync(i => i.ProductId == productId, cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<List<ProductImage>>(images));
    }

    public async Task<(HttpStatusCode, IResult)> AddImageAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _imageDal.AddAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.Created, new SuccessResult("Görsel eklendi."));
    }

    public async Task<(HttpStatusCode, IResult)> DeleteImageAsync(int imageId, CancellationToken cancellationToken = default)
    {
        var image = await _imageDal.GetAsync(i => i.Id == imageId, cancellationToken);
        if (image is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Görsel bulunamadı."));
        }

        _imageDal.Delete(image);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Görsel silindi."));
    }

    private Task<string> UniqueSlugAsync(string name, int excludedId, CancellationToken cancellationToken)
        => SlugGenerator.MakeUniqueAsync(
            SlugGenerator.Generate(name),
            async slug => await _productDal.GetAsync(p => p.Slug == slug && p.Id != excludedId, cancellationToken) is not null);
}
