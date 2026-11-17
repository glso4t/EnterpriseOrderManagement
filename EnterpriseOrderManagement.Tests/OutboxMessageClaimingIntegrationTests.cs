
using EnterpriseOrderManagement.Api.BackgroundServices;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;

using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;


//Δημιουργούμε 10 pending messages και δύο ανεξάρτητα service scopes.
//Κάθε scope έχει το δικό του DbContext και το δικό του OutboxMessageClaimService.
//Τα δύο claim operations εκτελούνται ταυτόχρονα και ζητούν από 5 μηνύματα.
//Αναμενόμενο αποτέλεσμα:
//Claim A 5 Messages, Claim B 5 Messages
public class OutboxMessageClaimingIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly SqlServerTestWebApplicationFactory _factory;

    public OutboxMessageClaimingIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task ConcurrentClaimers_ShouldNotClaimSameMessages()
    {
        // Arrange: create 10 pending messages.
        var nowUtc = DateTimeOffset.UtcNow;

        var messages = Enumerable.Range(0, 10)
            .Select(index => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "tests.concurrent-claim.v1",
                Content = "{}",
                OccurredOnUtc = nowUtc.AddMilliseconds(index)
            })
            .ToList();

        using (var seedScope = _factory.Services.CreateScope())
        {
            var dbContext = seedScope.ServiceProvider
                .GetRequiredService<EnterpriseOrderManagementDbContext>();

            await dbContext.OutboxMessages.AddRangeAsync(messages);

            await dbContext.SaveChangesAsync();
        }

        // Create two independent Dependency Injection scopes.
        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();

        var claimerA = scopeA.ServiceProvider
            .GetRequiredService<OutboxMessageClaimService>();

        var claimerB = scopeB.ServiceProvider
            .GetRequiredService<OutboxMessageClaimService>();

        // Act: both claimers request 5 messages concurrently.
        var claims = await Task.WhenAll(
            claimerA.ClaimPendingMessagesAsync(
                5,
                TimeSpan.FromMinutes(1),
                CancellationToken.None),

            claimerB.ClaimPendingMessagesAsync(
                5,
                TimeSpan.FromMinutes(1),
                CancellationToken.None)
        );

        var firstClaim = claims[0];
        var secondClaim = claims[1];

        var firstIds = firstClaim
            .Select(message => message.Id)
            .ToHashSet();

        var secondIds = secondClaim
            .Select(message => message.Id)
            .ToHashSet();

        // Assert: each claimer receives 5 messages.
        Assert.Equal(5, firstClaim.Count);
        Assert.Equal(5, secondClaim.Count);

        // No message can belong to both claimers.
        Assert.Empty(firstIds.Intersect(secondIds));

        // All claimed messages must have a lease and token.
        Assert.All(firstClaim.Concat(secondClaim), message =>
        {
            Assert.NotNull(message.LockedUntilUtc);
            Assert.NotNull(message.LockToken);
            Assert.Null(message.ProcessedOnUtc);
        });

        // Each batch must have a different claim token.
        Assert.NotEqual(
            firstClaim[0].LockToken,
            secondClaim[0].LockToken);
    }
}
