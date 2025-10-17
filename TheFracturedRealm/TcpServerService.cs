using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheFracturedRealm;

public sealed class TcpServerService : BackgroundService
{
    private readonly ILogger<TcpServerService> _log;
    private readonly Channel<InboundMessage> _inbound;
    private readonly World _world;
    private TcpListener? _listener;
    private const int Port = 4000;
    public TcpServerService(ILogger<TcpServerService> log, Channel<InboundMessage> inbound, World world)
    {
        _log = log;
        _inbound = inbound;
        _world = world;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _log.LogInformation("Listening on 0.0.0.0:{Port}", Port);
        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(stoppingToken);
                client.NoDelay = true;
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            _ = HandleClientAsync(client, stoppingToken);
        }
    }
    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var session = new Session(client);
        _world.Add(session);
        using var _ = client;
        using var stream = session.Stream;
        var writerTask = Task.Run(() => WriterLoopAsync(session, ct), ct);
        await SendWelcomeAsync(session, ct);
        var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        try
        {
            while (!ct.IsCancellationRequested && client.Connected)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null)
                {
                    break;
                }
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                await _inbound.Writer.WriteAsync(new InboundMessage(session, line), ct);
            }
        }
        catch (IOException) { /* connection dropped */ }
        catch (ObjectDisposedException) { /* shutting down */ }
        finally
        {
            _world.Remove(session);
            _world.Broadcast($"{Ansi.Dim}* {session} has left the world.{Ansi.Reset}");
            session.OutboundWriter.TryComplete();
            _log.LogInformation("Disconnected {Session}", session);
        }
        await writerTask;
    }
    private async Task WriterLoopAsync(Session session, CancellationToken ct)
    {
        var writer = new StreamWriter(session.Stream, new UTF8Encoding(false))
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        await foreach (var msg in session.OutboundReader.ReadAllAsync(ct))
        {
            try
            {
                var text = msg.Line.Replace("\r\n", "\n").Replace("\r", "").Replace("\n", writer.NewLine);
                await writer.WriteAsync(text);
                await writer.WriteAsync(writer.NewLine);
            }
            catch
            {
                break;
            }
        }
    }
    private async Task SendWelcomeAsync(Session s, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }
        _ = s.OutboundWriter.TryWrite(new OutboundMessage($"{Ansi.Green}Welcome to The Fractured Realm!{Ansi.Reset}"));
        _ = s.OutboundWriter.TryWrite(new OutboundMessage($"Your handle? Type: {Ansi.Yellow}name <yourname>{Ansi.Reset}"));
        _world.Broadcast($"{Ansi.Dim}* A new presence tingles at the edge of reality...{Ansi.Reset}", except: s);
        await Task.CompletedTask;
    }
}
