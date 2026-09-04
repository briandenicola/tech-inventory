namespace TechInventory.Domain.Enums;

/// <summary>
/// Which authentication entity a key acts on behalf of (ADR 0003).
/// <see cref="Owner"/> and <see cref="LocalUser"/> are distinct entities —
/// a <c>LocalUser</c> is a break-glass credential account and is never an
/// <c>Owner</c> row — so a key stores an explicit discriminator alongside
/// its principal id rather than a single foreign key.
/// </summary>
public enum ApiKeyPrincipalType
{
    /// <summary>An Entra-backed <c>Owner</c>.</summary>
    Owner = 1,

    /// <summary>An F025 break-glass <c>LocalUser</c>.</summary>
    LocalUser = 2,
}
