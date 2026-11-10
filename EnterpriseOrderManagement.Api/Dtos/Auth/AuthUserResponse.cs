namespace EnterpriseOrderManagement.Api.DTOs.Auth;

public class AuthUserResponse
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}