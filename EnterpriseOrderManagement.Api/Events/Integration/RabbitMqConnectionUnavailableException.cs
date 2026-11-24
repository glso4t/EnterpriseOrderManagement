namespace EnterpriseOrderManagement.Api.Events.Integration;

public sealed class RabbitMqConnectionUnavailableException
    : Exception
{
    public RabbitMqConnectionUnavailableException(string message)
        : base(message)
    {
    }
}