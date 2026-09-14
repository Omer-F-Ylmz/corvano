using Corvano.Business.Utilities;

namespace Corvano.Tests.Business;

public sealed class SizeAdvisorTests
{
    [Theory]
    [InlineData(171, 64, "S")]   // iki ölçü de S'nin üst sınırının hemen altında
    [InlineData(172, 64, "M")]   // boy sınırı: 172 artık M
    [InlineData(171, 65, "M")]   // kilo sınırı: 65 artık M
    [InlineData(183, 70, "L")]   // boy M'yi aşar, büyük olan kazanır
    [InlineData(176, 90, "XL")]  // kilo belirleyici
    [InlineData(130, 70, null)]  // tablo dışı boy: öneri yok
    public void Recommends_the_larger_of_the_height_and_weight_sizes(int heightCm, int weightKg, string? expected)
    {
        Assert.Equal(expected, SizeAdvisor.Recommend(heightCm, weightKg));
    }
}
