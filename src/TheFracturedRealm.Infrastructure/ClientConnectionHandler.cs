using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheFracturedRealm.Application.Abstractions;
using TheFracturedRealm.Domain;

namespace TheFracturedRealm.Infrastructure;

public sealed class ClientConnectionHandler : IClientConnectionHandler
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMudClientPool _clients;
    private readonly ILogger<ClientConnectionHandler> _logger;
    public ClientConnectionHandler(
        IMudClientPool clients,
        ILogger<ClientConnectionHandler> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _clients = clients;
        _logger = logger;
    }
    public Task HandleClient(TcpClient tcpClient, CancellationToken ct)
    {
        var mudClient = new MudClient(tcpClient);
        var added = _clients.TryAddClient(mudClient);
        if (!added)
        {
            _logger.LogWarning("Client {ClientId} already exists.", mudClient.Id);
            return Task.CompletedTask;
        }
        return HandleClientAsync(mudClient, ct);
    }
    private async Task HandleClientAsync(MudClient mudClient, CancellationToken ct)
    {
        try
        {
            await HandleConnectionAsync(mudClient, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client {ClientId}.", mudClient.Id);
        }
        finally
        {
            RemoveClient(mudClient.Id);
            mudClient.Dispose();
        }
    }
    private void RemoveClient(Guid clientId)
    {
        var removed = _clients.TryRemoveClient(clientId);
        if (!removed)
        {
            _logger.LogWarning("Client {ClientId} not found.", clientId);
        }
    }
    private async Task HandleConnectionAsync(MudClient mudClient, CancellationToken ct)
    {
        try
        {
            var stream = mudClient.GetStream();
            using var scope = _serviceScopeFactory.CreateScope();
            var requestProcessor = scope.ServiceProvider.GetRequiredService<ClientRequestProcessor>();
            await requestProcessor.ProcessRequestsAsync(stream, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error handling connection for client {ClientId}.", mudClient.Id);
        }
    }
}
