using Corvano.Business.Utilities;

namespace Corvano.Tests.Business;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Keten Gömlek", "keten-gomlek")]
    [InlineData("Şık İçlik", "sik-iclik")]
    [InlineData("Çağrı Öz Ürünü", "cagri-oz-urunu")]
    [InlineData("Yağmurluk ÜĞİŞÖÇ", "yagmurluk-ugisoc")]
    public void Generate_maps_turkish_letters_to_ascii(string input, string expected)
        => Assert.Equal(expected, SlugGenerator.Generate(input));

    [Fact]
    public void Generate_collapses_separators_and_trims_edges()
        => Assert.Equal("yun-pantolon", SlugGenerator.Generate("  Yün   Pantolon!!  "));
}
