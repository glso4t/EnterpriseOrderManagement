namespace EnterpriseOrderManagement.Api.BackgroundServices;

public enum OutboxCircuitState
{
    Closed,
    Open,
    HalfOpen
}