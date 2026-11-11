namespace EnterpriseOrderManagement.Api.Models;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    // We store the hash, never the original token.
    public string TokenHash { get; set; } = string.Empty;

    // Identifies all tokens belonging to the same login session.
    public Guid TokenFamilyId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    // Hash of the token that replaced this one.
    public string? ReplacedByTokenHash { get; set; }
}