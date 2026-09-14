namespace Corvano.Business.Utilities;

/// <summary>appsettings "Cart" bölümü.</summary>
public sealed class CartOptions
{
    public decimal ShippingFee { get; set; } = 149.90m;

    /// <summary>Ara toplam bu tutara ulaşınca kargo ücretsiz.</summary>
    public decimal FreeShippingThreshold { get; set; } = 2500m;

    public int CookieDays { get; set; } = 30;
}
