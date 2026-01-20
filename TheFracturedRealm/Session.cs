using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace TheFracturedRealm;

internal sealed class Session
{
    private readonly Channel<OutboundMessage> _outbound;
    public Guid Id { get; } = Guid.NewGuid();
    public string? Name { get; set; }
    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public EndPoint? RemoteEndPoint { get; }
    public ChannelReader<OutboundMessage> OutboundReader => _outbound.Reader;
    public ChannelWriter<OutboundMessage> OutboundWriter => _outbound.Writer;
    public Session(TcpClient client)
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _outbound = Channel.CreateUnbounded<OutboundMessage>(options);
        Client = client;
        Stream = client.GetStream();
        RemoteEndPoint = client.Client.RemoteEndPoint;
    }
    public override string ToString() => Name is { Length: > 0 } ? Name : $"#{Id.ToString()[..8]}";
}
