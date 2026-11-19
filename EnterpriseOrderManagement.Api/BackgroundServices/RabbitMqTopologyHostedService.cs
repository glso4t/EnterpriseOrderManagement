
using EnterpriseOrderManagement.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.BackgroundServices;

public sealed class RabbitMqTopologyHostedService(
    RabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqTopologyHostedService> logger)
    : IHostedService
{
    private const string ExchangeName = "enterprise.events";
    private const string QueueName = "notification-service";
    private const string RoutingKeyPattern = "order.#";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Initializing RabbitMQ topology...");

        var connection = await connectionManager.GetConnectionAsync(
            cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync( //Δημιουργεί το enterprise.events ως Topic Exchange
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(    //Δημιουργεί την notification-service
            queue: QueueName,
            durable: true,   //παραμένει μετά από restart.
            exclusive: false,   //δεν ανήκει αποκλειστικά σε μία σύνδεση.
            autoDelete: false,  //δεν διαγράφεται αυτόματα όταν αποσυνδεθεί ο Consumer.
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(   //Συνδέει την Queue με το Exchange μέσω του routing pattern order.#.
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKeyPattern, 
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "RabbitMQ topology initialized successfully. " +
            "Exchange: {Exchange}, Queue: {Queue}, Binding: {Binding}",
            ExchangeName,
            QueueName,
            RoutingKeyPattern);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "RabbitMQ topology hosted service is stopping.");
        return Task.CompletedTask;
    }
}
