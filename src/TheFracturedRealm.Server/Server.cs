using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TheFracturedRealm.Server;

internal sealed class Server
{
    private readonly Assembly _assembly = Assembly.Load("TheFracturedRealm.Application");
    private readonly ConcurrentDictionary<Guid, MudClient> _clients;
    private readonly TcpListener _listener;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public Server(TcpListener listener, IServiceScopeFactory serviceScopeFactory, ILogger logger)
    {
        _listener = listener;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _clients = new ConcurrentDictionary<Guid, MudClient>();
    }
    public async Task RunAsync(CancellationToken ct)
    {
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _logger.Information("Server started on port {Port}.", port);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);
                var mudClient = new MudClient(tcpClient);
                var added = _clients.TryAdd(mudClient.Id, mudClient);
                if (!added)
                {
                    _logger.Warning("Client {ClientId} already exists.", mudClient.Id);
                    mudClient.Dispose();
                    continue;
                }
                _ = HandleClientAsync(mudClient, ct);
            }
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAsync(MudClient mudClient, CancellationToken ct)
    {
        try
        {
            await HandleConnectionAsync(mudClient, ct);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing client {ClientId}.", mudClient.Id);
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
                    _logger.Information("Received: {Request}", request);
                    var alias = request.Split(' ')[0];
                    var args = request[alias.Length..].Trim();
                    var parts = alias.Split('-');
                    var command = string.Join("", parts.Select(p => char.ToUpper(p[0], CultureInfo.InvariantCulture) + p[1..]));
                    var type = _assembly.GetType($"TheFracturedRealm.Application.Commands.{command}Command");
                    if (type is null)
                    {
                        _logger.Warning("Command {Command} not found.", command);
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Command not found.\n"), ct);
                        continue;
                    }
                    var instance = Activator.CreateInstance(type, args);
                    if (instance is null)
                    {
                        _logger.Error("Error creating instance of {Type}.", type.Name);
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Error creating instance.\n"), ct);
                        continue;
                    }
                    await sender.Send(instance, ct);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error(ex, "Error handling connection for client {ClientId}.", mudClient.Id);
        }
    }
    private void RemoveClient(Guid clientId)
    {
        if (_clients.TryRemove(clientId, out var client))
        {
            client.Dispose();
            _logger.Information("Client {ClientId} disconnected and resources cleaned up.", clientId);
        }
    }
}
