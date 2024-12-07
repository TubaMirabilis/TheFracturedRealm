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
    private readonly IRequestRegistry _requestRegistry;
    public ClientConnectionHandler(
        IMudClientPool clients,
        ILogger<ClientConnectionHandler> logger,
        IServiceScopeFactory serviceScopeFactory,
        IRequestRegistry requestRegistry)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _clients = clients;
        _logger = logger;
        _requestRegistry = requestRegistry;
    }
    private async Task HandleConnectionAsync(MudClient mudClient, CancellationToken ct)
    {
        try
        {
            var stream = mudClient.GetStream();
            using var scope = _serviceScopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var readBuffer = new byte[1024];
            var messageBuffer = new List<byte>();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), ct)) != 0)
            {
                messageBuffer.AddRange(readBuffer.AsSpan(0, bytesRead).ToArray());
                int delimiterIndex;
                while ((delimiterIndex = messageBuffer.IndexOf((byte)'\n')) != -1)
                {
                    var messageBytes = messageBuffer.Take(delimiterIndex).ToArray();
                    messageBuffer.RemoveRange(0, delimiterIndex + 1);

                    var requestLine = Encoding.UTF8.GetString(messageBytes).TrimEnd('\r');
                    _logger.LogInformation("Received: {Request}", requestLine);

                    var parts = requestLine.Split(' ', 2);
                    var alias = parts[0];
                    var args = parts.Length > 1 ? parts[1] : string.Empty;

                    if (!_requestRegistry.TryCreateRequest(alias, args, out var request))
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Syntax Error.\n"), ct);
                        continue;
                    }

                    await sender.Send(request, ct);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error handling connection for client {ClientId}.", mudClient.Id);
        }
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
}
