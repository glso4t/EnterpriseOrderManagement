using EnterpriseOrderManagement.Api.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EnterpriseOrderManagement.Api.Services;
using EnterpriseOrderManagement.Api.Services.Interfaces;

//Διαβάζει τις ρυθμίσεις από το IOptions<RabbitMqSettings>.
//Ελέγχει αν υπάρχει ήδη ανοιχτή σύνδεση.
//Αν υπάρχει, την επαναχρησιμοποιεί.
//Αν δεν υπάρχει, δημιουργεί ConnectionFactory με τα credentials μας.
//Δημιουργεί τη σύνδεση με CreateConnectionAsync().
//Όταν τερματιστεί η εφαρμογή, κλείνει και αποδεσμεύει τη σύνδεση.
public sealed class RabbitMqConnectionManager(
    IOptions<RabbitMqSettings> options)
    : IAsyncDisposable, IRabbitMqConnectionManager
{
    private readonly RabbitMqSettings _settings = options.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,    //βοηθά στην επανασύνδεση μετά από απώλεια μιας ήδη επιτυχημένης σύνδεσης
                ClientProvidedName = "EnterpriseOrderManagement.Api"
            };

            _connection = await factory.CreateConnectionAsync(
                cancellationToken);
            return _connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            if (_connection.IsOpen)
            {
                await _connection.CloseAsync();
            }

            await _connection.DisposeAsync();
            _connection = null;
        }
        _semaphore.Dispose();
    }
}
