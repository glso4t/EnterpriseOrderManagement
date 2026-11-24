using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.Services.Interfaces;


//Provides the application's shared RabbitMQ connection.
public interface IRabbitMqConnectionManager
{
    Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default);
}