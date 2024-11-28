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
    private readonly List<TypeInfo> _requestTypes;
    private readonly ILogger<ClientConnectionHandler> _logger;
    public ClientConnectionHandler(IMudClientPool clients, ILogger<ClientConnectionHandler> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _clients = clients;
        _logger = logger;
        var assembly = Assembly.Load("TheFracturedRealm.Application");
        _requestTypes = assembly.DefinedTypes.Where(t => t.IsAssignableTo(typeof(IRequest<>))).ToList();
    }

    public Task HandleClient(TcpClient tcpClient, CancellationToken ct)
    {
        using var mudClient = new MudClient(tcpClient);
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
        }
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
                    var request = Encoding.UTF8.GetString(messageBytes).TrimEnd('\r');
                    _logger.LogInformation("Received: {Request}", request);
                    var alias = request.Split(' ')[0];
                    var args = request[alias.Length..].Trim();
                    var parts = alias.Split('-');
                    var command = string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..])) + "Command";
                    var query = string.Join("", parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..])) + "Query";
                    var type = _requestTypes.SingleOrDefault(t => t.Name == command || t.Name == query);
                    if (type is null)
                    {
                        _logger.LogWarning("Command {Command} not found.", command);
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Command not found.\n"), ct);
                        continue;
                    }
                    var instance = Activator.CreateInstance(type, args);
                    if (instance is null)
                    {
                        _logger.LogError("Error creating instance of {Type}.", type.Name);
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Syntax Error.\n"), ct);
                        continue;
                    }
                    await sender.Send(instance, ct);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error handling connection for client {ClientId}.", mudClient.Id);
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
