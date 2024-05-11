using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;

namespace TheFracturedRealm.Server;

public sealed class Server
{
    private readonly TcpListener _listener;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<Guid, MudClient> _clients;
    public Server(TcpListener listener, IServiceScopeFactory serviceScopeFactory, ILogger logger)
    {
        _listener = listener;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _clients = new ConcurrentDictionary<Guid, MudClient>();
    }
    public async Task RunAsync()
    {
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _logger.Information("Server started on port {Port}.", port);
        try
        {
            while (true)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                var mudClient = new MudClient(tcpClient);
                _clients.TryAdd(mudClient.Id, mudClient);
                await Task.Run(() => ProcessClient(mudClient))
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            _logger.Error(t.Exception, "Error processing client {ClientId}.", mudClient.Id);
                        }
                        RemoveClient(mudClient.Id);
                    }, TaskScheduler.Default);
            }
        }
        finally
        {
            _listener.Stop();
        }
    }
    private async Task ProcessClient(MudClient mudClient)
    {
        try
        {
            await HandleConnectionAsync(mudClient);
        }
        finally
        {
            mudClient.Dispose();
        }
    }
    private void RemoveClient(Guid clientId)
    {
        if (_clients.TryRemove(clientId, out MudClient? client))
        {
            client.Dispose(); // Ensure all resources are released
            _logger.Information("Client {ClientId} disconnected and resources cleaned up.", clientId);
        }
    }
    private async Task HandleConnectionAsync(MudClient mudClient)
    {
        using var stream = mudClient.GetStream();
        using var scope = _serviceScopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var assembly = Assembly.Load("TheFracturedRealm.Application");
        var buffer = new byte[1024];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
        {
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            _logger.Information("Received: {Request}", request);
            var alias = request.Split(' ')[0];
            var args = request.Substring(alias.Length).Trim();
            var parts = alias.Split('-');
            var command = string.Join("", parts.Select(p => char.ToUpper(p[0], CultureInfo.InvariantCulture) + p[1..]));
            var type = assembly.GetType($"MyMud.Application.Commands.{command}Command");
            if (type is null)
            {
                _logger.Warning("Command {Command} not found.", command);
                await stream.WriteAsync(Encoding.UTF8.GetBytes("Command not found."));
                continue;
            }
            var instance = Activator.CreateInstance(type, args);
            if (instance is null)
            {
                _logger.Error("Error creating instance of {Type}.", type.Name);
                await stream.WriteAsync(Encoding.UTF8.GetBytes("Error creating instance."));
                continue;
            }
            await sender.Send(instance);
        }
    }
}
