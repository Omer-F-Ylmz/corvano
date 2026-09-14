namespace Corvano.Business.Utilities;

/// <summary>Bir bedenin kapsadığı boy ve kilo aralığı; üst sınırlar dahil.</summary>
public sealed record SizeRow(string Size, int MinHeightCm, int MaxHeightCm, int MinWeightKg, int MaxWeightKg);

/// <summary>Boy ve kilodan beden önerir; tek kaynak <see cref="Table"/>. Boy ve kilo farklı beden gösterirse büyük olan önerilir.</summary>
public static class SizeAdvisor
{
    public static IReadOnlyList<SizeRow> Table { get; } =
    [
        new("S", 160, 171, 50, 64),
        new("M", 172, 178, 65, 74),
        new("L", 179, 185, 75, 84),
        new("XL", 186, 192, 85, 94),
        new("XXL", 193, 205, 95, 120)
    ];

    public static string? Recommend(int heightCm, int weightKg)
    {
        var byHeight = IndexOf(heightCm, row => (row.MinHeightCm, row.MaxHeightCm));
        var byWeight = IndexOf(weightKg, row => (row.MinWeightKg, row.MaxWeightKg));
        return byHeight < 0 || byWeight < 0 ? null : Table[Math.Max(byHeight, byWeight)].Size;
    }

    private static int IndexOf(int value, Func<SizeRow, (int Min, int Max)> range)
    {
        for (var i = 0; i < Table.Count; i++)
        {
            var (min, max) = range(Table[i]);
            if (value >= min && value <= max)
            {
                return i;
            }
        }

        return -1;
    }
}
