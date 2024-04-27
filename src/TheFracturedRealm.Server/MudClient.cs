using System.Net.Sockets;

namespace TheFracturedRealm.Server;

public class MudClient
{
    private readonly TcpClient _tcpClient;
    public MudClient(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
    }
    public string? PlayerName { get; set; }
    public NetworkStream GetStream() => _tcpClient.GetStream();
}
