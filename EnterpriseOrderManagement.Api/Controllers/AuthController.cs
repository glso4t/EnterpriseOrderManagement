
using System.IdentityModel.Tokens.Jwt;

using EnterpriseOrderManagement.Api.DTOs.Auth;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IConfiguration _configuration;

    private const string RefreshCookieName = "eom_refresh";

    private const string RefreshCookiePath = "/api/v1/auth";

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IConfiguration configuration
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _configuration = configuration;
    }

    // POST ((REGISTER)): /api/v1/auth/register
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request
    )
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };
        var createResult = await _userManager.CreateAsync(
            user,
            request.Password
        );
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(
                error => error.Description
            );
            if (createResult.Errors.Any(
                error =>
                    error.Code == "DuplicateEmail" ||
                    error.Code == "DuplicateUserName"
            ))
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }
            return BadRequest(new
            {
                errors
            });
        }
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "User"
        );
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "The account could not be assigned its default role."
                }
            );
        }
        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message = "Registration successful. Please log in."
            }
        );
    }

    // POST ((LOGIN)): /api/v1/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request
    )
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email
        );
        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }
        var signInResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true
            );
        if (!signInResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }
        var roles = await _userManager.GetRolesAsync(user);
        var refreshToken = await _refreshTokenService.CreateAsync(
            user.Id
        );

        SetRefreshCookie(
            refreshToken.Token,
            refreshToken.ExpiresAtUtc
        );
        var tokenResult = _jwtTokenService.CreateAccessToken(
            user,
            roles
        );
        var response = new AuthResponse
        {
            AccessToken = tokenResult.AccessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = tokenResult.ExpiresAtUtc,
            User = new AuthUserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User"
            }
        };
        return Ok(response);
    }

    private void SetRefreshCookie(
            string token,
            DateTime expiresAtUtc)
        {
            Response.Cookies.Append(
                RefreshCookieName,
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,

                    Expires = new DateTimeOffset(expiresAtUtc),

                    Path = RefreshCookiePath,

                    IsEssential = true
                }
            );
        }


    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(
            RefreshCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,

                Path = RefreshCookiePath
            }
        );
    }

    private bool IsTrustedRefreshRequest()
    {
        var customHeader =
            Request.Headers["X-Refresh-Request"].ToString();

        if (customHeader != "1")
        {
            return false;
        }

        var requestOrigin =
            Request.Headers["Origin"].ToString();

        var allowedOrigins = _configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? Array.Empty<string>();

        return allowedOrigins.Contains(
            requestOrigin,
            StringComparer.OrdinalIgnoreCase
        );
    }

    // GET: /api/v1/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>> GetCurrentUser()
    {
        var userId = User.FindFirst(
            JwtRegisteredClaimNames.Sub
        )?.Value;
        if (userId is null)
        {
            return Unauthorized();
        }
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }
        var roles = await _userManager.GetRolesAsync(user);
        var response = new AuthUserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "User"
        };
        return Ok(response);
    }
[AllowAnonymous]
[HttpPost("refresh")]
public async Task<ActionResult<AuthResponse>> Refresh()
{
    if (!IsTrustedRefreshRequest())
    {
        return Forbid();
    }

    var rawToken = Request.Cookies[RefreshCookieName];

    if (string.IsNullOrWhiteSpace(rawToken))
    {
        return Unauthorized();
    }

    var rotationResult =
        await _refreshTokenService.RotateAsync(rawToken);

    if (
        rotationResult.Status != RefreshTokenRotationStatus.Success ||
        rotationResult.UserId is null ||
        rotationResult.NewToken is null ||
        rotationResult.ExpiresAtUtc is null
    )
    {
        ClearRefreshCookie();

        return Unauthorized(new
        {
            message = "The refresh session is invalid or expired."
        });
    }

    var user = await _userManager.FindByIdAsync(
        rotationResult.UserId
    );

    if (
        user is null ||
        await _userManager.IsLockedOutAsync(user)
    )
    {
        await _refreshTokenService.RevokeFamilyAsync(
            rotationResult.NewToken
        );

        ClearRefreshCookie();

        return Unauthorized();
    }

    var roles = await _userManager.GetRolesAsync(user);

    var tokenResult = _jwtTokenService.CreateAccessToken(
        user,
        roles
    );

    SetRefreshCookie(
        rotationResult.NewToken,
        rotationResult.ExpiresAtUtc.Value
    );

    var response = new AuthResponse
    {
        AccessToken = tokenResult.AccessToken,
        TokenType = "Bearer",
        ExpiresAtUtc = tokenResult.ExpiresAtUtc,
        User = new AuthUserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? "User"
        }
    };

    return Ok(response);
}


[AllowAnonymous]
[HttpPost("logout")]
public async Task<IActionResult> Logout()
{
    if (!IsTrustedRefreshRequest())
    {
        return Forbid();
    }

    var rawToken = Request.Cookies[RefreshCookieName];

    if (!string.IsNullOrWhiteSpace(rawToken))
    {
        await _refreshTokenService.RevokeFamilyAsync(rawToken);
    }

    ClearRefreshCookie();

    return NoContent();
}
}