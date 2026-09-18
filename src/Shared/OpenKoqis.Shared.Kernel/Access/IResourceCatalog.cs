namespace OpenKoqis.Shared.Kernel.Access;

public interface IResourceCatalog
{
    IReadOnlySet<string> All { get; }
}
