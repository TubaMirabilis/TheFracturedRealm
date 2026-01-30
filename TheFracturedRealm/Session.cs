using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace TheFracturedRealm;

internal sealed class Session
{
    private readonly Channel<OutboundMessage> _outbound;
    public Guid Id { get; init; }
    public string? Name { get; set; }
    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public EndPoint? RemoteEndPoint { get; }
    public ChannelReader<OutboundMessage> OutboundReader => _outbound.Reader;
    public ChannelWriter<OutboundMessage> OutboundWriter => _outbound.Writer;
    public Session(TcpClient client)
    {
        var options = new BoundedChannelOptions(250)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        };
        _outbound = Channel.CreateBounded<OutboundMessage>(options);
        Client = client;
        Stream = client.GetStream();
        RemoteEndPoint = client.Client.RemoteEndPoint;
    }
    public void EnqueueWelcomeMessages()
    {
        OutboundWriter.TryWrite(new OutboundMessage($"{Ansi.Green}Welcome to The Fractured Realm!{Ansi.Reset}"));
        OutboundWriter.TryWrite(new OutboundMessage($"Your handle? Type: {Ansi.Yellow}name <yourname>{Ansi.Reset}"));
    }
    public void Close()
    {
        try
        {
            OutboundWriter.TryComplete();
            Client.Close();
        }
        catch
        {
            // Suppress exceptions during forceful closure
        }
    }
    public override string ToString() => Name is { Length: > 0 } ? Name : $"#{Id.ToString()[..8]}";
}
