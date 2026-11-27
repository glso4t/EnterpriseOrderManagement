namespace EnterpriseOrderManagement.Api.BackgroundServices;

/// <summary>
/// Circuit breaker for the sequential Outbox publisher worker.
/// This class is owned and accessed by a single worker loop.
/// </summary>
public sealed class OutboxCircuitBreaker
{
    private readonly TimeProvider _timeProvider;
    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;

    private int _consecutiveFailures;
    private long? _openedAtTimestamp;

    public OutboxCircuitBreaker(
        TimeProvider timeProvider,
        int failureThreshold = 3,
        TimeSpan? breakDuration = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (failureThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureThreshold));
        }

        var duration =
            breakDuration ?? TimeSpan.FromSeconds(30);

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(breakDuration));
        }

        _timeProvider = timeProvider;
        _failureThreshold = failureThreshold;
        _breakDuration = duration;
    }

    public int ConsecutiveFailures =>
        _consecutiveFailures;

    public OutboxCircuitState State
    {
        get
        {
            if (_openedAtTimestamp is null)
            {
                return OutboxCircuitState.Closed;
            }

            var elapsed = _timeProvider.GetElapsedTime(
                _openedAtTimestamp.Value);

            return elapsed >= _breakDuration
                ? OutboxCircuitState.HalfOpen
                : OutboxCircuitState.Open;
        }
    }

    public bool CanAttempt =>
        State != OutboxCircuitState.Open;

    public void RecordSuccess()
    {
        _consecutiveFailures = 0;
        _openedAtTimestamp = null;
    }

    public void RecordFailure(
        OutboxFailureKind failureKind)
    {
        if (failureKind == OutboxFailureKind.Message)
        {
            // Invalid messages do not provide evidence
            // about the availability of the broker.
            return;
        }

        if (State == OutboxCircuitState.Open)
        {
            return;
        }

        if (State == OutboxCircuitState.HalfOpen)
        {
            OpenCircuit();
            return;
        }

        _consecutiveFailures++;

        if (_consecutiveFailures >= _failureThreshold)
        {
            OpenCircuit();
        }
    }

    private void OpenCircuit()
    {
        _consecutiveFailures = _failureThreshold;

        _openedAtTimestamp =
            _timeProvider.GetTimestamp();
    }
}