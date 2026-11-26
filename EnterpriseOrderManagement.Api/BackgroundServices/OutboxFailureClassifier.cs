using System.IO;
using System.Net.Sockets;
using EnterpriseOrderManagement.Api.Events.Integration;
using RabbitMQ.Client.Exceptions;

namespace EnterpriseOrderManagement.Api.BackgroundServices;


//Κεντρική ταξινόμηση αποτυχιών
//Χρησιμοποιούμε ένα switch expression, το οποίο εξετάζει τον πραγματικό τύπο του exception και επιστρέφει την κατάλληλη κατηγορία
public static class OutboxFailureClassifier
{
    public static OutboxFailureKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            InvalidIntegrationMessageException
                => OutboxFailureKind.Message,

            RabbitMqConnectionUnavailableException
                => OutboxFailureKind.Infrastructure,

            BrokerUnreachableException
                => OutboxFailureKind.Infrastructure,

            OperationInterruptedException
                => OutboxFailureKind.Infrastructure,

            PublishException
                => OutboxFailureKind.Infrastructure,

            TimeoutException
                => OutboxFailureKind.Infrastructure,

            SocketException
                => OutboxFailureKind.Infrastructure,

            IOException
                => OutboxFailureKind.Infrastructure,

            OperationCanceledException
                => OutboxFailureKind.Infrastructure,

            _ => OutboxFailureKind.Unknown  //λειτουργεί ως προεπιλεγμένη περίπτωση. Αν δεν αναγνωρίζουμε το exception, επιστρέφουμε Unknown
        };
    }
}