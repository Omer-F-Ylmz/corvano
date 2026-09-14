using System.Globalization;
using System.Net;
using Corvano.Business.Abstract;
using Corvano.Business.Utilities;
using Corvano.Core.DataAccess;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;
using Corvano.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Business.Concrete;

public class CartManager : ICartService
{
    private const string UnavailableMessage = "Bu parça artık satışta değil; toplama dahil edilmedi.";

    private readonly ICartDal _cartDal;
    private readonly ICartItemDal _itemDal;
    private readonly IProductVariantDal _variantDal;
    private readonly IProductDal _productDal;
    private readonly ICategoryDal _categoryDal;
    private readonly IProductImageDal _imageDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CartOptions _options;

    public CartManager(
        ICartDal cartDal,
        ICartItemDal itemDal,
        IProductVariantDal variantDal,
        IProductDal productDal,
        ICategoryDal categoryDal,
        IProductImageDal imageDal,
        IUnitOfWork unitOfWork,
        CartOptions options)
    {
        _cartDal = cartDal;
        _itemDal = itemDal;
        _variantDal = variantDal;
        _productDal = productDal;
        _categoryDal = categoryDal;
        _imageDal = imageDal;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<(HttpStatusCode, IDataResult<CartDto>)> GetCartAsync(Guid? cartKey, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(cartKey, cancellationToken);
        if (cart is null)
        {
            return (HttpStatusCode.OK, new SuccessDataResult<CartDto>(new CartDto(null, [], 0m, 0m, 0m, 0)));
        }

        var items = await _itemDal.GetListAsync(i => i.CartId == cart.Id, cancellationToken);
        var lines = await LinesAsync(items.OrderBy(i => i.AddedAt).ToList(), refreshPrices: true, cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<CartDto>(Summarize(cart.CartKey, lines)));
    }

    public async Task<int> GetItemCountAsync(Guid? cartKey, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(cartKey, cancellationToken);
        return cart is null ? 0 : (await _itemDal.GetListAsync(i => i.CartId == cart.Id, cancellationToken)).Sum(i => i.Quantity);
    }

    public async Task<(HttpStatusCode, IDataResult<CartAddResultDto>)> AddAsync(Guid? cartKey, int variantId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
        {
            return (HttpStatusCode.BadRequest, new ErrorDataResult<CartAddResultDto>("Adet en az 1 olmalı."));
        }

        var variant = await _variantDal.GetAsync(v => v.Id == variantId, cancellationToken);
        var product = variant is null ? null : await _productDal.GetAsync(p => p.Id == variant.ProductId, cancellationToken);
        if (variant is null || product is null)
        {
            return (HttpStatusCode.NotFound, new ErrorDataResult<CartAddResultDto>("Seçilen beden bulunamadı."));
        }

        if (!product.IsActive)
        {
            return (HttpStatusCode.Conflict, new ErrorDataResult<CartAddResultDto>(UnavailableMessage));
        }

        if (variant.Stock == 0)
        {
            return (HttpStatusCode.Conflict, new ErrorDataResult<CartAddResultDto>($"{product.Name} ({variant.Size}) tükendi."));
        }

        var cart = await FindCartAsync(cartKey, cancellationToken);
        var created = cart is null;
        if (cart is null)
        {
            cart = new Cart { CartKey = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            await _cartDal.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var cartId = cart.Id;
        var unitPrice = variant.PriceOverride ?? product.Price;
        CartItem item;
        bool clamped;
        try
        {
            (item, clamped) = await MergeAsync(cartId, variant, quantity, unitPrice, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // eşzamanlı ekleme aynı satırı az önce yazdı (UNIQUE cart_id + product_variant_id): o satıra ekle
            (item, clamped) = await MergeAsync(cartId, variant, quantity, unitPrice, cancellationToken);
        }

        var allItems = await _itemDal.GetListAsync(i => i.CartId == cartId, cancellationToken);
        var line = (await LinesAsync([item], refreshPrices: false, cancellationToken)).Single();
        var summary = Summarize(cart.CartKey, await LinesAsync(allItems, refreshPrices: false, cancellationToken));
        var message = clamped
            ? $"Stokta yalnız {variant.Stock} adet var; sepetinizdeki adet {variant.Stock} olarak sabitlendi."
            : $"{product.Name} ({variant.Size}) sepete eklendi.";

        return (created ? HttpStatusCode.Created : HttpStatusCode.OK,
            new SuccessDataResult<CartAddResultDto>(new CartAddResultDto(cart.CartKey, line, summary.ItemCount, summary.Subtotal, clamped), message));
    }

    public async Task<(HttpStatusCode, IResult)> UpdateQuantityAsync(Guid? cartKey, int itemId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
        {
            return (HttpStatusCode.BadRequest, new ErrorResult("Adet negatif olamaz."));
        }

        var item = await FindItemAsync(cartKey, itemId, cancellationToken);
        if (item is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Sepet satırı bulunamadı."));
        }

        if (quantity == 0)
        {
            _itemDal.Delete(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (HttpStatusCode.OK, new SuccessResult("Satır sepetten çıkarıldı."));
        }

        var variant = await _variantDal.GetAsync(v => v.Id == item.ProductVariantId, cancellationToken);
        var stock = variant?.Stock ?? 0;
        if (quantity > stock)
        {
            return (HttpStatusCode.Conflict, new ErrorResult($"Stokta {stock} adet var; en fazla {stock} adet seçebilirsiniz."));
        }

        item.Quantity = quantity;
        _itemDal.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Adet güncellendi."));
    }

    public async Task<(HttpStatusCode, IResult)> RemoveAsync(Guid? cartKey, int itemId, CancellationToken cancellationToken = default)
    {
        var item = await FindItemAsync(cartKey, itemId, cancellationToken);
        if (item is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Sepet satırı bulunamadı."));
        }

        _itemDal.Delete(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Satır sepetten çıkarıldı."));
    }

    private async Task<(CartItem Item, bool Clamped)> MergeAsync(int cartId, ProductVariant variant, int quantity, decimal unitPrice, CancellationToken cancellationToken)
    {
        var existing = await _itemDal.GetTrackedAsync(i => i.CartId == cartId && i.ProductVariantId == variant.Id, cancellationToken);
        var wanted = (existing?.Quantity ?? 0) + quantity;
        var clamped = wanted > variant.Stock;
        var finalQuantity = Math.Min(wanted, variant.Stock);

        if (existing is not null)
        {
            existing.Quantity = finalQuantity;
            existing.UnitPrice = unitPrice;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (existing, clamped);
        }

        var item = new CartItem
        {
            CartId = cartId,
            ProductVariantId = variant.Id,
            Quantity = finalQuantity,
            UnitPrice = unitPrice,
            AddedAt = DateTime.UtcNow
        };
        await _itemDal.AddAsync(item, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _itemDal.Detach(item);
            throw;
        }

        return (item, clamped);
    }

    private async Task<Cart?> FindCartAsync(Guid? cartKey, CancellationToken cancellationToken)
        => cartKey is { } key ? await _cartDal.GetAsync(c => c.CartKey == key, cancellationToken) : null;

    private async Task<CartItem?> FindItemAsync(Guid? cartKey, int itemId, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(cartKey, cancellationToken);
        return cart is null ? null : await _itemDal.GetTrackedAsync(i => i.Id == itemId && i.CartId == cart.Id, cancellationToken);
    }

    private async Task<List<CartLineDto>> LinesAsync(List<CartItem> items, bool refreshPrices, CancellationToken cancellationToken)
    {
        var variantIds = items.Select(i => i.ProductVariantId).ToList();
        var variants = await _variantDal.GetListAsync(v => variantIds.Contains(v.Id), cancellationToken);
        var productIds = variants.Select(v => v.ProductId).ToList();
        var products = await _productDal.GetListAsync(p => productIds.Contains(p.Id), cancellationToken);
        var images = await _imageDal.GetListAsync(i => productIds.Contains(i.ProductId), cancellationToken);
        var categories = await _categoryDal.GetListAsync(cancellationToken: cancellationToken);

        var lines = new List<CartLineDto>();
        var changed = false;
        foreach (var item in items)
        {
            var variant = variants.First(v => v.Id == item.ProductVariantId);
            var product = products.First(p => p.Id == variant.ProductId);
            var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
            var top = category?.ParentId is { } parentId ? categories.FirstOrDefault(c => c.Id == parentId) : category;
            var currentPrice = variant.PriceOverride ?? product.Price;

            var priceUpdated = false;
            if (refreshPrices && product.IsActive && currentPrice != item.UnitPrice)
            {
                item.UnitPrice = currentPrice;
                _itemDal.Update(item);
                priceUpdated = changed = true;
            }

            string? warning = null;
            if (!product.IsActive)
            {
                warning = UnavailableMessage;
            }
            else if (item.Quantity > variant.Stock)
            {
                warning = variant.Stock == 0
                    ? "Bu beden tükendi; satırı çıkarın ya da başka beden seçin."
                    : $"Stokta yalnız {variant.Stock} adet kaldı; adedi azaltın.";
            }
            else if (priceUpdated)
            {
                warning = $"Fiyat güncellendi: {currentPrice.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} ₺.";
            }

            lines.Add(new CartLineDto(
                item.Id,
                variant.Id,
                product.Name,
                product.Slug,
                top?.Slug ?? string.Empty,
                variant.Size,
                images.Where(i => i.ProductId == product.Id).OrderBy(i => i.Url.Contains("-dark.", StringComparison.Ordinal)).ThenBy(i => i.SortOrder).FirstOrDefault(),
                item.Quantity,
                item.UnitPrice,
                variant.Stock,
                product.IsActive,
                priceUpdated,
                warning));
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return lines;
    }

    private CartDto Summarize(Guid cartKey, List<CartLineDto> lines)
    {
        var subtotal = lines.Sum(l => l.LineTotal);
        var shipping = subtotal == 0m || subtotal >= _options.FreeShippingThreshold ? 0m : _options.ShippingFee;
        var itemCount = lines.Where(l => l.IsAvailable).Sum(l => l.Quantity);
        return new CartDto(cartKey, lines, subtotal, shipping, subtotal + shipping, itemCount);
    }
}
