
using System.Text;
using EnterpriseOrderManagement.Api.Models;
using EnterpriseOrderManagement.Api.Services;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.Events.Integration;

public sealed class RabbitMqIntegrationEventPublisher(
    RabbitMqConnectionManager connectionManager,
    ILogger<RabbitMqIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    private const string ExchangeName = "enterprise.events";

    // A single message must be confirmed within this time.
    private static readonly TimeSpan PublishTimeout =
        TimeSpan.FromSeconds(5);

    public async Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.Type))
        {
            throw new ArgumentException(
                "Outbox message Type cannot be empty.",
                nameof(message));
        }

        // The Outbox event Type is also our routing key.
        string routingKey = message.Type;

        // The timeout covers connection, channel creation and publishing.
        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutCts.CancelAfter(PublishTimeout);

        try
        {
            var connection =
                await connectionManager.GetConnectionAsync( //Παίρνει τη σύνδεση από τον ήδη υπάρχοντα RabbitMqConnectionManager. Δεν δημιουργεί δεύτερο ConnectionFactory
                    timeoutCts.Token);

            var channelOptions = new CreateChannelOptions(  //Δημιουργεί channel με Publisher Confirms ενεργά.
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);    // επιτρέπει στον client να συσχετίζει τα returns με τα αντίστοιχα μηνύματα

            await using var channel =
                await connection.CreateChannelAsync(
                    channelOptions,
                    timeoutCts.Token);

            byte[] body = Encoding.UTF8.GetBytes(message.Content);  //Μετατρέπει το JSON σε bytes, επειδή το RabbitMQ μεταφέρει το body ως bytes

            var properties = new BasicProperties
            {
                MessageId = message.Id.ToString(),
                Type = message.Type,    //ο τύπος του integration event.
                ContentType = "application/json",
                Persistent = true,
                Timestamp = new AmqpTimestamp(
                    message.OccurredOnUtc.ToUnixTimeSeconds())  //η αρχική ημερομηνία του event.
            };

            // This await completes when the broker confirms the message.
            // mandatory=true also makes an unroutable message fail.
            await channel.BasicPublishAsync(    //Κάνει publish με mandatory: true. Αν δεν υπάρχει κατάλληλο binding,
                exchange: ExchangeName,         // το BasicPublishAsync αποτυγχάνει αντί να θεωρήσει επιτυχημένο το unroutable μήνυμα.
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: timeoutCts.Token);   //Περιμένει το confirm. Αν δεν ολοκληρωθεί μέσα σε 5 δευτερόλεπτα, επιστρέφει TimeoutException. Το Outbox worker θα το αντιμετωπίσει ως αποτυχία και θα καλέσει MarkFailedAsync.

            logger.LogInformation(
                "Published Outbox message {MessageId} with routing key {RoutingKey}.",
                message.Id,
                routingKey);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested &&
                timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"RabbitMQ did not confirm Outbox message {message.Id} " +
                $"within {PublishTimeout}.");
        }
    }
}
