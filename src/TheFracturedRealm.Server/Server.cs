using System.Net;
using System.Net.Sockets;
using Serilog;
using TheFracturedRealm.Application.Abstractions;

namespace TheFracturedRealm.Server;

internal sealed class Server
{
    private readonly TcpListener _listener;
    private readonly ILogger _logger;
    private readonly IClientConnectionHandler _clientConnectionHandler;
    public Server(TcpListener listener, ILogger logger, IClientConnectionHandler clientConnectionHandler)
    {
        _listener = listener;
        _logger = logger;
        _clientConnectionHandler = clientConnectionHandler;
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
                _ = _clientConnectionHandler.HandleClient(tcpClient, ct);
            }
        }
        finally
        {
            _listener.Stop();
        }
    }
}
