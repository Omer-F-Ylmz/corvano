using System.Globalization;
using Corvano.Business.Utilities;
using Corvano.Entities.Concrete;
using Corvano.Entities.Dtos;

namespace Corvano.Web.Models;

public sealed record HomeViewModel(List<ProductCardDto> Featured, List<HomeCategoryDto> Categories);

public sealed record ProductViewModel(ProductPageDto Page, int? HeightCm, int? WeightKg, List<ProductCardDto> Similar)
{
    public string TopCategorySlug => (Page.ParentCategory ?? Page.Category).Slug;

    public bool IsAccessory => TopCategorySlug == "aksesuar";

    public string? RecommendedSize => HeightCm is { } height && WeightKg is { } weight
        ? SizeAdvisor.Recommend(height, weight)
        : null;

    public FabricLabel Fabric => ProductDescription.Parse(Page.Product.Description);

    /// <summary>[ÖNERİ] Üst kategoriye göre giyim notu; ürün bazında alan D3 şemasında yok.</summary>
    public string HowToWear => TopCategorySlug switch
    {
        "gomlek" => "Yakayı bir düğme açık bırakın, kolu iki kez katlayın. Keten kırışır; bu onun karakteri, kusuru değil.",
        "pantolon" => "Paça ayakkabının üstüne bir kez değsin. Pilesi olan pantolonu kemersiz, belde oturtarak giyin.",
        "dis-giyim" => "Altına ince bir triko alın. Omuz dikişi kendi omzunuzun bittiği yerde bitmeli, bir parmak ötede değil.",
        "triko" => "Gömlek yakasını içeride bırakın ya da tek başına, düz bir kumaş pantolonla giyin.",
        _ => "Yaka çiçeğini sol yakanın ilik hizasına takın. Kravatın ucu kemer tokasına değsin, geçmesin."
    };
}

/// <summary>Ürün görsel kabı: gerçek fotoğraf ya da kategori silüeti.</summary>
public sealed record VisualModel(ProductImage? Image, string CategorySlug, string Name, bool Large = false, bool Lazy = true, string? CssClass = null);

public static class Storefront
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    /// <summary>Koyu zeminde basit parça silüetleri (viewBox 0 0 400 500).</summary>
    private static readonly Dictionary<string, string> Silhouettes = new()
    {
        ["gomlek"] = "M150 70 L200 92 L250 70 L326 108 L372 236 L322 256 L296 186 L296 440 L104 440 L104 186 L78 256 L28 236 L74 108 Z M170 80 L200 132 L230 80 M200 132 V430",
        ["pantolon"] = "M118 60 H282 L306 450 H220 L200 176 L180 450 H94 Z M118 92 H282 M200 92 V150",
        ["dis-giyim"] = "M146 52 L200 76 L254 52 L324 94 L356 322 L312 332 L300 206 L306 462 L94 462 L100 206 L88 332 L44 322 L76 94 Z M160 62 L200 230 L240 62 M200 230 V452",
        ["triko"] = "M150 86 Q200 122 250 86 L330 118 L372 338 L326 350 L300 222 L300 410 Q200 426 100 410 L100 222 L74 350 L28 338 L70 118 Z M104 390 Q200 406 296 390"
    };

    /// <summary>"1.890,00 ₺"</summary>
    public static string Price(decimal price) => $"{price.ToString("N2", Turkish)} ₺";

    /// <summary>Yalnız atölyede işlenmiş yerel fotoğraflar gerçek görsel sayılır; diğerleri silüetle gösterilir.</summary>
    public static bool IsPhoto(ProductImage? image) => image is not null && image.Url.StartsWith("/img/", StringComparison.Ordinal);

    public static string Thumb(string url) => url.Replace(".webp", "-thumb.webp", StringComparison.Ordinal);

    public static string Silhouette(string categorySlug) => Silhouettes.GetValueOrDefault(categorySlug, Silhouettes["gomlek"]);

    /// <summary>Sayfa zeminine uyan fotoğraf önce: koyu sayfada "-dark" eşi, krem sayfada açık zeminli olan.</summary>
    public static List<ProductImage> ForTheme(IEnumerable<ProductImage> images, bool dark)
        => images.OrderByDescending(i => i.Url.Contains("-dark.", StringComparison.Ordinal) == dark).ThenBy(i => i.SortOrder).ToList();
}
