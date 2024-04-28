using System.Net.Sockets;
using TheFracturedRealm.Domain;

namespace TheFracturedRealm.Server;

public class MudClient
{
    private readonly TcpClient _tcpClient;
    public MudClient(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
    }
    public Character? Character { get; set; }
    public NetworkStream GetStream() => _tcpClient.GetStream();
}
