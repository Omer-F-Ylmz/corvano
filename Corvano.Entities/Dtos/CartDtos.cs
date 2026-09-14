using Corvano.Entities.Concrete;

namespace Corvano.Entities.Dtos;

/// <summary>Sepet satırı; satışta olmayan satır toplama girmez.</summary>
public sealed record CartLineDto(
    int ItemId,
    int ProductVariantId,
    string ProductName,
    string ProductSlug,
    string CategorySlug,
    string Size,
    ProductImage? Image,
    int Quantity,
    decimal UnitPrice,
    int Stock,
    bool IsAvailable,
    bool PriceUpdated,
    string? Warning)
{
    public decimal LineTotal => IsAvailable ? UnitPrice * Quantity : 0m;

    public bool IsLowStock => IsAvailable && Stock is > 0 and <= 3;
}

public sealed record CartDto(Guid? CartKey, List<CartLineDto> Lines, decimal Subtotal, decimal Shipping, decimal Total, int ItemCount)
{
    public bool IsEmpty => Lines.Count == 0;
}

public sealed record CartAddResultDto(Guid CartKey, CartLineDto Line, int ItemCount, decimal Subtotal, bool WasClamped);
