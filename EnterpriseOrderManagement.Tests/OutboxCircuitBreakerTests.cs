using EnterpriseOrderManagement.Api.BackgroundServices;

namespace EnterpriseOrderManagement.Tests;

public class OutboxCircuitBreakerTests
{
    [Fact]
    public void InitialState_IsClosed() //Ελέγχει :Αρχική κατάσταση
    {
        var (_, breaker) = CreateHarness();

        Assert.Equal(
            OutboxCircuitState.Closed,
            breaker.State);

        Assert.True(breaker.CanAttempt);
        Assert.Equal(0, breaker.ConsecutiveFailures);
    }

    [Fact]
    public void InfrastructureFailures_BelowThreshold_RemainClosed()    //Ελέγχει :Αποτυχίες κάτω από το όριο
    {
        var (_, breaker) = CreateHarness();

        breaker.RecordFailure(
            OutboxFailureKind.Infrastructure);

        breaker.RecordFailure(
            OutboxFailureKind.Infrastructure);

        Assert.Equal(
            OutboxCircuitState.Closed,
            breaker.State);

        Assert.Equal(2, breaker.ConsecutiveFailures);
        Assert.True(breaker.CanAttempt);
    }

    [Fact]
    public void InfrastructureFailures_AtThreshold_OpenCircuit()    //Ελέγχει :Άνοιγμα circuit
    {
        var (_, breaker) = CreateHarness();

        OpenCircuit(breaker);

        Assert.Equal(
            OutboxCircuitState.Open,
            breaker.State);

        Assert.False(breaker.CanAttempt);
        Assert.Equal(3, breaker.ConsecutiveFailures);
    }

    [Fact]
    public void MessageFailures_DoNotOpenCircuit()  //Ελέγχει :Απομόνωση προβληματικών μηνυμάτων από το circuit
    {
        var (_, breaker) = CreateHarness();

        for (int i = 0; i < 10; i++)
        {
            breaker.RecordFailure(
                OutboxFailureKind.Message);
        }

        Assert.Equal(
            OutboxCircuitState.Closed,
            breaker.State);

        Assert.Equal(0, breaker.ConsecutiveFailures);
        Assert.True(breaker.CanAttempt);
    }

    [Fact]
    public void UnknownFailures_CanOpenCircuit()    //Ελέγχει :Προστασία από άγνωστες συστημικές αποτυχίες
    {
        var (_, breaker) = CreateHarness();

        breaker.RecordFailure(
            OutboxFailureKind.Infrastructure);

        breaker.RecordFailure(
            OutboxFailureKind.Unknown);

        breaker.RecordFailure(
            OutboxFailureKind.Unknown);

        Assert.Equal(
            OutboxCircuitState.Open,
            breaker.State);

        Assert.False(breaker.CanAttempt);
    }

    [Fact]
    public void OpenCircuit_BecomesHalfOpen_AfterBreakDuration()    //Έλεγχος cooldown
    {
        var (clock, breaker) = CreateHarness();

        OpenCircuit(breaker);

        clock.Advance(TimeSpan.FromSeconds(29));

        Assert.Equal(
            OutboxCircuitState.Open,
            breaker.State);

        Assert.False(breaker.CanAttempt);

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(
            OutboxCircuitState.HalfOpen,
            breaker.State);

        Assert.True(breaker.CanAttempt);

        // An invalid message is not a successful broker probe.
        breaker.RecordFailure(
            OutboxFailureKind.Message);

        Assert.Equal(
            OutboxCircuitState.HalfOpen,
            breaker.State);
    }

    [Fact]
    public void HalfOpen_ProbeFailure_ReopensCircuit()  //Ελέγχει :Αποτυχημένη δοκιμαστική δημοσίευση
    {
        var (clock, breaker) = CreateHarness();

        OpenCircuit(breaker);

        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(
            OutboxCircuitState.HalfOpen,
            breaker.State);

        breaker.RecordFailure(
            OutboxFailureKind.Infrastructure);

        Assert.Equal(
            OutboxCircuitState.Open,
            breaker.State);

        clock.Advance(TimeSpan.FromSeconds(29));

        Assert.False(breaker.CanAttempt);

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(
            OutboxCircuitState.HalfOpen,
            breaker.State);
    }

    [Fact]
    public void SuccessfulPublish_ResetsFailureCount_AndClosesCircuit() //Ελέγχει : Επιτυχής ανάκαμψη
    {
        var (clock, breaker) = CreateHarness();

        breaker.RecordFailure(
            OutboxFailureKind.Infrastructure);

        breaker.RecordSuccess();

        Assert.Equal(0, breaker.ConsecutiveFailures);

        OpenCircuit(breaker);

        clock.Advance(TimeSpan.FromSeconds(30));

        breaker.RecordSuccess();

        Assert.Equal(
            OutboxCircuitState.Closed,
            breaker.State);

        Assert.Equal(0, breaker.ConsecutiveFailures);
        Assert.True(breaker.CanAttempt);
    }

    private static void OpenCircuit(
        OutboxCircuitBreaker breaker)
    {
        for (int i = 0; i < 3; i++)
        {
            breaker.RecordFailure(
                OutboxFailureKind.Infrastructure);
        }
    }

    private static (
        ManualTimeProvider Clock,
        OutboxCircuitBreaker Breaker)
        CreateHarness()
    {
        var clock = new ManualTimeProvider();

        var breaker = new OutboxCircuitBreaker(
            clock,
            failureThreshold: 3,
            breakDuration: TimeSpan.FromSeconds(30));

        return (clock, breaker);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency =>
            TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            return _timestamp;
        }

        public void Advance(TimeSpan duration)
        {
            _timestamp += duration.Ticks;
        }
    }
}