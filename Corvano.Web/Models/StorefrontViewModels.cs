using System.Globalization;
using Corvano.Business.Utilities;
using Corvano.Entities.Concrete;
using Corvano.Entities.Dtos;

namespace Corvano.Web.Models;

public sealed record HomeViewModel(List<HomeStripDto> Strips);

public sealed record ProductViewModel(ProductPageDto Page, int? HeightCm, int? WeightKg)
{
    public bool IsAccessory => (Page.ParentCategory ?? Page.Category).Slug == "aksesuar";

    public string? RecommendedSize => HeightCm is { } height && WeightKg is { } weight
        ? SizeAdvisor.Recommend(height, weight)
        : null;

    public FabricLabel Fabric => ProductDescription.Parse(Page.Product.Description);

    /// <summary>[ÖNERİ] Üst kategoriye göre giyim notu; ürün bazında alan D3 şemasında yok.</summary>
    public string HowToWear => (Page.ParentCategory ?? Page.Category).Slug switch
    {
        "gomlek" => "Yakayı bir düğme açık bırakın, kolu iki kez katlayın. Keten kırışır; bu onun karakteri, kusuru değil.",
        "pantolon" => "Paça ayakkabının üstüne bir kez değsin. Pilesi olan pantolonu kemersiz, belde oturtarak giyin.",
        "dis-giyim" => "Altına ince bir triko alın. Omuz dikişi kendi omzunuzun bittiği yerde bitmeli, bir parmak ötede değil.",
        "triko" => "Gömlek yakasını içeride bırakın ya da tek başına, düz bir kumaş pantolonla giyin.",
        _ => "Yaka çiçeğini sol yakanın ilik hizasına takın. Kravatın ucu kemer tokasına değsin, geçmesin."
    };
}

public static class Storefront
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    /// <summary>"1.890,00 ₺"</summary>
    public static string Price(decimal price) => $"{price.ToString("N2", Turkish)} ₺";

    /// <summary>Yerel webp için -thumb eşi; yer tutucu için 640x800 sürümü.</summary>
    public static string Thumb(string url) => url.StartsWith("/img/", StringComparison.Ordinal)
        ? url.Replace(".webp", "-thumb.webp", StringComparison.Ordinal)
        : url.Replace("1280x1600", "640x800", StringComparison.Ordinal);

    public static ProductImage? Primary(IEnumerable<ProductImage> images) => images.OrderBy(i => i.SortOrder).FirstOrDefault();
}
