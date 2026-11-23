using EnterpriseOrderManagement.Api.BackgroundServices;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public class OutboxMessageCompletionIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly SqlServerTestWebApplicationFactory _factory;

    public OutboxMessageCompletionIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task MarkProcessed_WithOwnedToken_MarksMessageAsProcessed()
    {
        var lockToken = Guid.NewGuid();
        var message = await SeedClaimedMessageAsync(lockToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var completionService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageCompletionService>();

            var marked = await completionService.MarkProcessedAsync(
                message.Id,
                lockToken,
                CancellationToken.None);

            Assert.True(marked);
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.NotNull(stored.ProcessedOnUtc);
        Assert.Null(stored.Error);
    }

    [Fact]
    public async Task MarkProcessed_WithWrongToken_DoesNotChangeMessage()
    {
        var message = await SeedClaimedMessageAsync(Guid.NewGuid());

        using (var scope = _factory.Services.CreateScope())
        {
            var completionService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageCompletionService>();

            var marked = await completionService.MarkProcessedAsync(
                message.Id,
                Guid.NewGuid(),
                CancellationToken.None);

            Assert.False(marked);
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.Null(stored.ProcessedOnUtc);
    }

    [Fact]
    public async Task MarkFailed_RecordsErrorAndKeepsMessageUnprocessed()
    {
        var lockToken = Guid.NewGuid();
        var message = await SeedClaimedMessageAsync(lockToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var completionService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageCompletionService>();

            var marked = await completionService.MarkFailedAsync(
                message.Id,
                lockToken,
                1,
                "broker unavailable",
                CancellationToken.None);

            Assert.True(marked);
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.Equal("broker unavailable", stored.Error);
        Assert.Null(stored.ProcessedOnUtc);
        Assert.Null(stored.LockToken);
        Assert.Null(stored.LockedUntilUtc);
    }

    [Fact]
    public async Task ExpiredLease_IsReclaimed_AndOldTokenCannotComplete()
    {
        var oldToken = Guid.NewGuid();
        var message = await SeedClaimedMessageAsync(oldToken);

        // Simulate a crashed worker: its lease has expired.
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<EnterpriseOrderManagementDbContext>();

            await dbContext.OutboxMessages
                .Where(stored => stored.Id == message.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    stored => stored.LockedUntilUtc,
                    (DateTimeOffset?)DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        using var workerScope = _factory.Services.CreateScope();

        var claimService = workerScope.ServiceProvider
            .GetRequiredService<OutboxMessageClaimService>();

        var completionService = workerScope.ServiceProvider
            .GetRequiredService<OutboxMessageCompletionService>();

        // A second worker claims the expired message with a new token.
        var reclaimed = await claimService.ClaimPendingMessagesAsync(
            10,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        var reclaimedMessage = Assert.Single(
            reclaimed,
            claimed => claimed.Id == message.Id);

        Assert.NotEqual(oldToken, reclaimedMessage.LockToken);

        // The crashed worker wakes up: its token is stale.
        Assert.False(await completionService.MarkProcessedAsync(
            message.Id,
            oldToken,
            CancellationToken.None));

        // The new owner can complete the message.
        Assert.True(await completionService.MarkProcessedAsync(
            message.Id,
            reclaimedMessage.LockToken!.Value,
            CancellationToken.None));
    }



    [Fact]
    public async Task ExpiredLease_CannotCompleteOrScheduleRetry()
    {
        var lockToken = Guid.NewGuid();
        var message = await SeedClaimedMessageAsync(lockToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<EnterpriseOrderManagementDbContext>();

            await dbContext.OutboxMessages
                .Where(stored => stored.Id == message.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    stored => stored.LockedUntilUtc,
                    (DateTimeOffset?)DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var completionService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageCompletionService>();

            Assert.False(await completionService.MarkProcessedAsync(
                message.Id,
                lockToken,
                CancellationToken.None));

            Assert.False(await completionService.MarkFailedAsync(
                message.Id,
                lockToken,
                1,
                "late failure from expired worker",
                CancellationToken.None));
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.Null(stored.ProcessedOnUtc);
        Assert.Null(stored.Error);
        Assert.Equal(lockToken, stored.LockToken);
        Assert.NotNull(stored.LockedUntilUtc);
        Assert.True(stored.LockedUntilUtc.Value < DateTimeOffset.UtcNow);
    }



    // Seeds a message that is already claimed (valid lease and token).
    private async Task<OutboxMessage> SeedClaimedMessageAsync(
        Guid lockToken)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "tests.completion.v1",
            Content = "{}",
            OccurredOnUtc = DateTimeOffset.UtcNow,
            LockToken = lockToken,
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(1)
        };

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<EnterpriseOrderManagementDbContext>();

        dbContext.OutboxMessages.Add(message);

        await dbContext.SaveChangesAsync();

        return message;
    }

    private async Task<OutboxMessage> ReadMessageAsync(Guid messageId)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<EnterpriseOrderManagementDbContext>();

        return await dbContext.OutboxMessages
            .AsNoTracking()
            .SingleAsync(stored => stored.Id == messageId);
    }
}