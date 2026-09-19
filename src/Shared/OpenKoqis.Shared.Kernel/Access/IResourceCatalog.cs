namespace OpenKoqis.Shared.Kernel.Access;

public interface IResourceCatalog
{
    IReadOnlySet<Resource> All { get; }
}
