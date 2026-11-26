using System.IO;
using System.Net.Sockets;
using EnterpriseOrderManagement.Api.BackgroundServices;
using EnterpriseOrderManagement.Api.Events.Integration;
using RabbitMQ.Client.Exceptions;

namespace EnterpriseOrderManagement.Tests;


//Unit Tests για τον Failure Classifier
public class OutboxFailureClassifierTests
{
    [Fact]
    public void Classify_InvalidMessage_ReturnsMessage()
    {
        var exception =
            new InvalidIntegrationMessageException(
                "Invalid JSON.");

        var result =
            OutboxFailureClassifier.Classify(exception);

        Assert.Equal(
            OutboxFailureKind.Message,
            result);
    }

    [Fact]
    public void Classify_ConnectionFailures_ReturnsInfrastructure()
    {
        Exception[] exceptions =
        [
            new RabbitMqConnectionUnavailableException(
                "Connection unavailable."),

            new BrokerUnreachableException(
                new Exception("Broker offline.")),

            new OperationInterruptedException()
        ];

        foreach (var exception in exceptions)
        {
            Assert.Equal(
                OutboxFailureKind.Infrastructure,
                OutboxFailureClassifier.Classify(exception));
        }
    }

    [Fact]
    public void Classify_NetworkFailures_ReturnsInfrastructure()
    {
        Exception[] exceptions =
        [
            new TimeoutException(),

            new IOException("Network error."),

            new SocketException(),

            new OperationCanceledException()
        ];

        foreach (var exception in exceptions)
        {
            Assert.Equal(
                OutboxFailureKind.Infrastructure,
                OutboxFailureClassifier.Classify(exception));
        }
    }

    [Fact]
    public void Classify_NackAndReturn_ReturnsInfrastructure()
    {
        var nack = new PublishException(
            publishSequenceNumber: 1,
            isReturn: false);

        var returned = new PublishReturnException(
            publishSequenceNumber: 2,
            message: "Unroutable message.");

        Assert.Equal(
            OutboxFailureKind.Infrastructure,
            OutboxFailureClassifier.Classify(nack));

        Assert.Equal(
            OutboxFailureKind.Infrastructure,
            OutboxFailureClassifier.Classify(returned));
    }

    [Fact]
    public void Classify_UnexpectedException_ReturnsUnknown()
    {
        Exception[] exceptions =
        [
            new InvalidOperationException(),

            new ArgumentException(),

            new NullReferenceException(),

            new Exception("Unexpected failure.")
        ];

        foreach (var exception in exceptions)
        {
            Assert.Equal(
                OutboxFailureKind.Unknown,
                OutboxFailureClassifier.Classify(exception));
        }
    }
}