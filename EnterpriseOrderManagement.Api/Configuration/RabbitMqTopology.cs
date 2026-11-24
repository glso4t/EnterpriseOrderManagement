
namespace EnterpriseOrderManagement.Api.Configuration;

public static class RabbitMqTopology
{
    public const string MainExchangeName = "enterprise.events";

    public const string MainQueueName = "notification-service";

    public const string MainRoutingKeyPattern = "orders.#";

    public const string DeadLetterExchangeName = "enterprise.events.dlx";

    public const string DeadLetterQueueName = "notification-service.dlq";

    public const string DeadLetterRoutingKey = "notification-service.dead";
}
