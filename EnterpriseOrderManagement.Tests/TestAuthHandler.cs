using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseOrderManagement.Tests;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        // Επιτρέπει σε συγκεκριμένο test να προσομοιώσει
        // request χωρίς πιστοποιημένο χρήστη
        if (Request.Headers.ContainsKey(
            "X-Test-Unauthenticated"))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }
        // Από προεπιλογή, τα tests χρησιμοποιούν Admin
        // Κάποιο test μπορεί να δηλώσει X-Test-Role: User
        var role = Request.Headers.TryGetValue(
            "X-Test-Role",
            out var requestedRole)
                ? requestedRole.ToString()
                : "Admin";
        var claims = new[]
        {
            new Claim(
                ClaimTypes.Name,
                "integration-test-user"),
            new Claim(
                ClaimTypes.Role,
                role)
        };
        var identity = new ClaimsIdentity(
            claims,
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(
            principal,
            Scheme.Name);   
        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}