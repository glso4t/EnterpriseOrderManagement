namespace EnterpriseOrderManagement.Api.Services.Auth;

public interface IRefreshTokenService
{
    Task<RefreshTokenIssue> CreateAsync(string userId);

    Task<RefreshTokenRotationResult> RotateAsync(string rawToken);

    Task RevokeFamilyAsync(string rawToken);
}