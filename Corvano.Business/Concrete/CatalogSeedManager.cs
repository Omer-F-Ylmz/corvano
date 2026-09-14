using System.Net;
using Corvano.Business.Abstract;
using Corvano.Business.Utilities;
using Corvano.Core.DataAccess;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Concrete;

/// <summary>Geliştirme kataloğu: 5 üst + 3 alt kategori, 12 ürün. Kayıtlar slug / SKU / görsel adresiyle eşlenir.</summary>
public class CatalogSeedManager : ICatalogSeedService
{
    private sealed record SeedProduct(
        string Category,
        string Name,
        string Code,
        decimal Price,
        string Description,
        string Color,
        (string Size, int Stock)[] Variants,
        (string Url, string Alt)[] Images);

    private static readonly (string Name, string? Parent)[] Categories =
    [
        ("Gömlek", null), ("Pantolon", null), ("Dış Giyim", null), ("Triko", null), ("Aksesuar", null),
        ("Yaka Çiçeği", "Aksesuar"), ("Kravat", "Aksesuar"), ("Broş & Zincir", "Aksesuar")
    ];

    private static readonly (string Size, int Stock)[] Sizes = [("S", 4), ("M", 9), ("L", 7), ("XL", 0)];

    private static readonly SeedProduct[] Products =
    [
        Clothing("Gömlek", "Yıkanmış Keten Gömlek", "KG", 1890m, "%100 keten | Türkiye | 30°C'de ters çevirip yıkayın, nemliyken ütüleyin", "Ekru"),
        Clothing("Gömlek", "Oxford Pamuk Gömlek", "OG", 1590m, "%100 pamuk oxford dokuma | Türkiye | 40°C'de yıkayın, orta ısıda ütüleyin", "Açık mavi"),
        Clothing("Pantolon", "Pileli Yün Pantolon", "PY", 2890m, "%70 yün, %30 polyester | Türkiye | Kuru temizleme; buharla düzeltin", "Antrasit"),
        Clothing("Pantolon", "Gabardin Chino Pantolon", "GC", 2190m, "%98 pamuk, %2 elastan gabardin | Türkiye | 30°C'de ters yıkayın, düşük ısıda ütüleyin", "Haki"),
        Clothing("Dış Giyim", "Kaşe Palto", "KP", 6990m, "%80 yün, %20 kaşmir | Türkiye | Yalnız kuru temizleme; geniş omuzlu askıda saklayın", "Lacivert"),
        Clothing("Dış Giyim", "Yün Flanel Blazer", "YB", 5490m, "%100 yün flanel | Türkiye | Kuru temizleme; giydikten sonra askıda havalandırın", "Koyu gri"),
        Clothing("Triko", "Merinos Balıkçı Yaka Triko", "MB", 2190m, "%100 merinos yünü | Türkiye | 30°C'de elde yıkayın, düz serip kurutun", "Kömür"),
        Clothing("Triko", "Kaşmir Karışımlı V Yaka Triko", "KV", 2690m, "%90 yün, %10 kaşmir | Türkiye | Elde yıkayın, sıkmadan düz kurutun", "Deve tüyü"),
        Accessory("Yaka Çiçeği", "Gri Gül Yaka Çiçeği", "YCG", 690m, "Köpük gül, tül ve saten kordon | Türkiye | Kutusunda, nemden uzak saklayın", "Gri",
            [("İğneli", 6), ("Klipsli", 3)], "yaka-cicegi-gri-1", "Gri köpük güller ve puantiyeli tülle sarılmış yaka çiçeği"),
        Accessory("Broş & Zincir", "Füme Zincirli Broş Takımı", "BZF", 890m, "Köpük gül, füme metal zincir | Türkiye | Zinciri kuru bezle silin, nemden uzak tutun", "Füme",
            [("Tek", 5), ("Çift", 2)], "bros-zincir-fume-1", "Siyah ve gri güllü broş, cebe uzanan füme zincirle"),
        Accessory("Kravat", "Gri Desenli İnce Kravat", "KRG", 790m, "%100 polyester jakar | Türkiye | Kuru temizleme; rulo yapıp saklayın", "Gri",
            [("6 cm", 8), ("7 cm", 4)], "kravat-gri-desenli-1", "Gri zemin üzerinde geometrik jakar desenli ince kravat"),
        Accessory("Kravat", "Kiremit İnce Kravat", "KRK", 790m, "%100 polyester örgü doku | Türkiye | Kuru temizleme; rulo yapıp saklayın", "Kiremit",
            [("6 cm", 6), ("7 cm", 0)], "kravat-kiremit-1", "Kiremit renkli, ince örgü dokulu kravat")
    ];

    private readonly ICategoryDal _categoryDal;
    private readonly IProductDal _productDal;
    private readonly IProductVariantDal _variantDal;
    private readonly IProductImageDal _imageDal;
    private readonly IUnitOfWork _unitOfWork;

    public CatalogSeedManager(
        ICategoryDal categoryDal,
        IProductDal productDal,
        IProductVariantDal variantDal,
        IProductImageDal imageDal,
        IUnitOfWork unitOfWork)
    {
        _categoryDal = categoryDal;
        _productDal = productDal;
        _variantDal = variantDal;
        _imageDal = imageDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<(HttpStatusCode, IResult)> EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        var added = 0;
        var categoryIds = new Dictionary<string, int>();
        for (var i = 0; i < Categories.Length; i++)
        {
            var (name, parent) = Categories[i];
            var slug = SlugGenerator.Generate(name);
            var category = await _categoryDal.GetAsync(c => c.Slug == slug, cancellationToken);
            if (category is null)
            {
                category = new Category
                {
                    Name = name,
                    Slug = slug,
                    ParentId = parent is null ? null : categoryIds[parent],
                    SortOrder = parent is null ? i + 1 : i - 4,
                    IsActive = true
                };
                await _categoryDal.AddAsync(category, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                added++;
            }

            categoryIds[name] = category.Id;
        }

        var createdAt = DateTime.UtcNow;
        for (var i = 0; i < Products.Length; i++)
        {
            var seed = Products[i];
            var slug = SlugGenerator.Generate(seed.Name);
            var product = await _productDal.GetAsync(p => p.Slug == slug, cancellationToken);
            if (product is null)
            {
                product = new Product
                {
                    Name = seed.Name,
                    Slug = slug,
                    Description = seed.Description,
                    CategoryId = categoryIds[seed.Category],
                    Price = seed.Price,
                    IsActive = true,
                    CreatedAt = createdAt.AddMinutes(-i),
                    UpdatedAt = createdAt.AddMinutes(-i)
                };
                await _productDal.AddAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                added++;
            }

            foreach (var (size, stock) in seed.Variants)
            {
                var sku = $"CRV-{seed.Code}-{SlugGenerator.Generate(size).ToUpperInvariant()}";
                if (await _variantDal.GetAsync(v => v.Sku == sku, cancellationToken) is null)
                {
                    await _variantDal.AddAsync(new ProductVariant { ProductId = product.Id, Size = size, Color = seed.Color, Sku = sku, Stock = stock }, cancellationToken);
                    added++;
                }
            }

            for (var order = 0; order < seed.Images.Length; order++)
            {
                var (url, alt) = seed.Images[order];
                var productId = product.Id;
                if (await _imageDal.GetAsync(img => img.ProductId == productId && img.Url == url, cancellationToken) is null)
                {
                    await _imageDal.AddAsync(new ProductImage { ProductId = productId, Url = url, Alt = alt, SortOrder = order }, cancellationToken);
                    added++;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return added == 0
            ? (HttpStatusCode.OK, new SuccessResult("Katalog zaten tohumlanmış."))
            : (HttpStatusCode.Created, new SuccessResult($"Katalog tohumlandı: {added} kayıt eklendi."));
    }

    private static SeedProduct Clothing(string category, string name, string code, decimal price, string description, string color)
        => new(category, name, code, price, description, color, Sizes,
        [
            (Placeholder(name, "Ön"), $"{name}, önden"),
            (Placeholder(name, "Detay"), $"{name}, kumaş detayı")
        ]);

    private static SeedProduct Accessory(
        string category, string name, string code, decimal price, string description, string color,
        (string Size, int Stock)[] variants, string photo, string alt)
        => new(category, name, code, price, description, color, variants,
        [
            ($"/img/products/{photo}.webp", alt),
            ($"/img/products/{photo}-dark.webp", $"{alt}, koyu zeminde")
        ]);

    /// <summary>Gerçek fotoğraf gelene kadar koyu zeminli yer tutucu (1280x1600, 4:5).</summary>
    private static string Placeholder(string name, string view)
        => $"https://placehold.co/1280x1600/14261e/f4f1ea/webp?text={Uri.EscapeDataString($"{name}\\n{view}")}";
}
