
using System.Security.Cryptography;
using System.Text;

using EnterpriseOrderManagement.Api.Configuration;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseOrderManagement.Api.Services.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly EnterpriseOrderManagementDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenService(
        EnterpriseOrderManagementDbContext dbContext,
        IOptions<JwtSettings> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtOptions.Value;
    }


//Δημιουργεί ένα τυχαίο token, υπολογίζει το hash του και αποθηκεύει μόνο το hash στη βάση.
    public async Task<RefreshTokenIssue> CreateAsync(string userId)
    {
        var now = DateTime.UtcNow;

        var rawToken = GenerateToken();
        var tokenHash = HashToken(rawToken);

        var expiresAt = now.AddDays(
            _jwtSettings.RefreshTokenExpirationDays
        );

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            TokenFamilyId = Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt
        };

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync();

        // Return the original token only once.
        // The database stores only its hash.
        return new RefreshTokenIssue(
            rawToken,
            expiresAt
        );
    }


//Βρίσκει το token, ελέγχει αν είναι έγκυρο, ακυρώνει το παλιό και αποθηκεύει το καινούργιο.
    public async Task<RefreshTokenRotationResult> RotateAsync(
        string rawToken)
    {
        var tokenHash = HashToken(rawToken);

        var existingToken = await _dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (existingToken is null)
        {
            return new RefreshTokenRotationResult(
                RefreshTokenRotationStatus.Invalid
            );
        }

        var now = DateTime.UtcNow;

        if (existingToken.ExpiresAtUtc <= now)
        {
            return new RefreshTokenRotationResult(
                RefreshTokenRotationStatus.Expired
            );
        }

        // An already revoked token was reused.
        if (existingToken.RevokedAtUtc is not null)
        {
            await RevokeActiveFamilyAsync(
                existingToken.TokenFamilyId,
                now
            );

            return new RefreshTokenRotationResult(
                RefreshTokenRotationStatus.ReuseDetected
            );
        }


        // Generate the next token before replacing the current one.
        var newRawToken = GenerateToken();
        var newTokenHash = HashToken(newRawToken);

        var newExpiresAt = now.AddDays(
            _jwtSettings.RefreshTokenExpirationDays
        );


        // The rotation must be atomic.
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();


        // Only one request can successfully revoke the old token.
        var updatedRows = await _dbContext.RefreshTokens
            .Where(x =>
                x.Id == existingToken.Id &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.RevokedAtUtc,
                    now
                )
                .SetProperty(
                    x => x.ReplacedByTokenHash,
                    newTokenHash
                ));


        if (updatedRows != 1)
        {
            // The token was already used by another request.
            await RevokeActiveFamilyAsync(
                existingToken.TokenFamilyId,
                now
            );

            await transaction.CommitAsync();

            return new RefreshTokenRotationResult(
                RefreshTokenRotationStatus.ReuseDetected
            );
        }


        // Create the replacement token in the same family.
        var replacementToken = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newTokenHash,
            TokenFamilyId = existingToken.TokenFamilyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = newExpiresAt
        };

        _dbContext.RefreshTokens.Add(replacementToken);

        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();


        return new RefreshTokenRotationResult(
            RefreshTokenRotationStatus.Success,
            existingToken.UserId,
            newRawToken,
            newExpiresAt
        );
    }


//Ακυρώνει όλα τα ενεργά tokens που ανήκουν στην ίδια συνεδρία.
    public async Task RevokeFamilyAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);

        var existingToken = await _dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (existingToken is null)
        {
            return;
        }

        await RevokeActiveFamilyAsync(
            existingToken.TokenFamilyId,
            DateTime.UtcNow
        );
    }


    private async Task RevokeActiveFamilyAsync(
        Guid familyId,
        DateTime revokedAt)
    {
        await _dbContext.RefreshTokens
            .Where(x =>
                x.TokenFamilyId == familyId &&
                x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.RevokedAtUtc,
                    revokedAt
                ));
    }


    private static string GenerateToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }


    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);

        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}
