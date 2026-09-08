namespace Corvano.Tests;

/// <summary>Tüm testler tek gerçek veritabanını paylaşır; sıralı koşarlar.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseCollection
{
    public const string Name = "database";
}
