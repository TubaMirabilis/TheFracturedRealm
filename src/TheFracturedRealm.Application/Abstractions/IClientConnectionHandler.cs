using System.Net.Sockets;

namespace TheFracturedRealm.Application.Abstractions;

public interface IClientConnectionHandler
{
    Task HandleClient(TcpClient tcpClient, CancellationToken ct);
}
