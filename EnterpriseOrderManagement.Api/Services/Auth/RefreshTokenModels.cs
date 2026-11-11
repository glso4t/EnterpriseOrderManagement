namespace EnterpriseOrderManagement.Api.Services.Auth;

public record RefreshTokenIssue(
    string Token,
    DateTime ExpiresAtUtc
);

public enum RefreshTokenRotationStatus
{
    Success,
    Invalid,
    Expired,
    ReuseDetected
}

public record RefreshTokenRotationResult(
    RefreshTokenRotationStatus Status,
    string? UserId = null,
    string? NewToken = null,
    DateTime? ExpiresAtUtc = null
);