using System.Net.Sockets;

namespace TheFracturedRealm.Application.Abstractions;

public interface IClientRequestProcessor
{
    Task ProcessRequestsAsync(NetworkStream stream, CancellationToken ct);
}
