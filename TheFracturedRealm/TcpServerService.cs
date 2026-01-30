using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheFracturedRealm;

internal sealed class TcpServerService : BackgroundService
{
    private readonly ILogger<TcpServerService> _log;
    private readonly Channel<InboundMessage> _inbound;
    private readonly World _world;
    private readonly TcpListener _listener;
    public TcpServerService(ILogger<TcpServerService> log, Channel<InboundMessage> inbound, World world)
    {
        _listener = new TcpListener(IPAddress.Any, 4000);
        _log = log;
        _inbound = inbound;
        _world = world;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start();
        _log.LogInformation("Listening on port 4000");
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
        session.EnqueueWelcomeMessages();
        _world.Broadcast($"{Ansi.Dim}* A new presence tingles at the edge of reality...{Ansi.Reset}", except: session);
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
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
        catch (IOException)
        {
            _log.LogWarning("Connection dropped for {Session}", session);
        }
        catch (ObjectDisposedException)
        {
            _log.LogWarning("Stream disposed for {Session}", session);
        }
        finally
        {
            _world.Remove(session);
            _world.Broadcast($"{Ansi.Dim}* {session} has left the world.{Ansi.Reset}");
            session.OutboundWriter.TryComplete();
            _log.LogInformation("Disconnected {Session}", session);
        }
        await writerTask;
    }
    private static async Task WriterLoopAsync(Session session, CancellationToken ct)
    {
        using var writer = new StreamWriter(session.Stream, new UTF8Encoding(false))
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        await foreach (var msg in session.OutboundReader.ReadAllAsync(ct))
        {
            try
            {
                var text = msg.Line.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "", StringComparison.Ordinal).Replace("\n", writer.NewLine, StringComparison.Ordinal);
                await writer.WriteAsync(text);
                await writer.WriteAsync(writer.NewLine);
            }
            catch
            {
                break;
            }
        }
    }
    public override void Dispose()
    {
        _listener.Stop();
        _listener.Dispose();
        base.Dispose();
    }
}
