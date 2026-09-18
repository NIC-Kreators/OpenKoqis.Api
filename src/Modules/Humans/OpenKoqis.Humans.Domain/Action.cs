namespace OpenKoqis.Humans.Domain;

[Flags]
public enum Action : byte
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    Edit = 1 << 2,
    Delete = 1 << 3,
}
