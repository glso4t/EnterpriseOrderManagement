using EnterpriseOrderManagement.Api.Models;

namespace EnterpriseOrderManagement.Api.Services.Auth;

public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(
        ApplicationUser user,
        IList<string> roles
    );
}