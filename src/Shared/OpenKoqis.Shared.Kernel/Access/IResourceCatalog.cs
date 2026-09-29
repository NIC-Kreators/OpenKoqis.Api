namespace OpenKoqis.Shared.Kernel.Access;

/// <summary>
/// Source of every <see cref="Resource"/> known to the system.
/// </summary>
public interface IResourceCatalog
{
    /// <summary>
    /// All registered resources.
    /// </summary>
    IReadOnlySet<Resource> All { get; }
}
