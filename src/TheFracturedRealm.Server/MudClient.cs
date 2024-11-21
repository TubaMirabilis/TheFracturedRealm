using System.Net.Sockets;
using System.Text;
using TheFracturedRealm.Domain;

namespace TheFracturedRealm.Server;

internal sealed class MudClient : IDisposable
{
    private readonly TcpClient _tcpClient;
    public MudClient(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        Id = Guid.NewGuid();
    }
    public Guid Id { get; private set; }
    public Character? Character { get; set; }
    public NetworkStream GetStream() => _tcpClient.GetStream();
    public async Task NotifyAsync(string message)
    {
        var stream = GetStream();
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(messageBytes);
    }
    public void Dispose() => _tcpClient.Dispose();
}
