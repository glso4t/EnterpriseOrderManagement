
using EnterpriseOrderManagement.Api.BackgroundServices;
using EnterpriseOrderManagement.Api.Data;
using EnterpriseOrderManagement.Api.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseOrderManagement.Tests;

public class OutboxMessageLeaseRenewalIntegrationTests
    : IClassFixture<SqlServerTestWebApplicationFactory>
{
    private readonly SqlServerTestWebApplicationFactory _factory;

    public OutboxMessageLeaseRenewalIntegrationTests(
        SqlServerTestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.InitializeDatabase();
    }

    [Fact]
    public async Task RenewLease_WithOwnedToken_ExtendsLease()
    {
        var lockToken = Guid.NewGuid();
        var message = await SeedClaimedMessageAsync(lockToken);
        var originalLeaseExpiry = message.LockedUntilUtc!.Value;

        using (var scope = _factory.Services.CreateScope())
        {
            var renewalService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageLeaseRenewalService>();

            var renewed = await renewalService.RenewLeaseAsync(
                message.Id,
                lockToken,
                TimeSpan.FromMinutes(5),
                CancellationToken.None);

            Assert.True(renewed);
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.NotNull(stored.LockedUntilUtc);
        Assert.True(
            stored.LockedUntilUtc.Value > originalLeaseExpiry.AddMinutes(3),
            "The renewed lease should be significantly later than the original expiry.");
        Assert.Equal(lockToken, stored.LockToken);
    }

    [Fact]
    public async Task RenewLease_WithWrongToken_DoesNotChangeLease()
    {
        var lockToken = Guid.NewGuid();
        var message = await SeedClaimedMessageAsync(lockToken);
        var originalLeaseExpiry = message.LockedUntilUtc;

        using (var scope = _factory.Services.CreateScope())
        {
            var renewalService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageLeaseRenewalService>();

            var renewed = await renewalService.RenewLeaseAsync(
                message.Id,
                Guid.NewGuid(),
                TimeSpan.FromMinutes(5),
                CancellationToken.None);

            Assert.False(renewed);
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.Equal(lockToken, stored.LockToken);
        Assert.Equal(originalLeaseExpiry, stored.LockedUntilUtc);
    }

    [Fact]
    public async Task RenewLease_AfterLeaseExpired_CannotResurrectClaim()
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
            var renewalService = scope.ServiceProvider
                .GetRequiredService<OutboxMessageLeaseRenewalService>();

            var renewed = await renewalService.RenewLeaseAsync(
                message.Id,
                lockToken,
                TimeSpan.FromMinutes(5),
                CancellationToken.None);

            Assert.False(renewed);
        }

        var stored = await ReadMessageAsync(message.Id);

        Assert.Equal(lockToken, stored.LockToken);
        Assert.NotNull(stored.LockedUntilUtc);
        Assert.True(stored.LockedUntilUtc.Value < DateTimeOffset.UtcNow);
        Assert.Null(stored.ProcessedOnUtc);
    }

    private async Task<OutboxMessage> SeedClaimedMessageAsync(
        Guid lockToken)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "tests.lease-renewal.v1",
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
