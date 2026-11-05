namespace OpenKoqis.Humans.Application;

/// <summary>
/// Configuration of the Humans module, bound from the <see cref="SectionName"/> section.
/// </summary>
public sealed record HumansOptions
{
    public const string SectionName = "Humans";

    /// <summary>
    /// Whether OpenKoqis may create identities in the identity provider.
    /// When <c>false</c>, users can only be linked to identities that already exist there.
    /// </summary>
    public bool IsIdentityCreationAllowed { get; init; }
}
