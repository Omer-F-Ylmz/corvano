namespace Corvano.Business.Utilities;

public sealed record FabricLabel(string Fabric, string? Origin, string? Care);

/// <summary>Ürün açıklaması "kumaş | menşei | bakım" biçimindedir; ayraç yoksa tümü kumaş sayılır.</summary>
public static class ProductDescription
{
    public static FabricLabel Parse(string description)
    {
        var parts = description.Split('|', StringSplitOptions.TrimEntries);
        return new FabricLabel(
            parts[0],
            parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null,
            parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null);
    }
}
