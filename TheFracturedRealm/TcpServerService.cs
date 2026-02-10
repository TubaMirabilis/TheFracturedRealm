using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, Task> _clientTasks = new();
    private readonly PeriodicTimer _pruneTimer = new(TimeSpan.FromMinutes(1));
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
        var pruneTask = PruneCompletedTasksAsync(stoppingToken);
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
            var sessionId = Guid.NewGuid();
            var task = HandleClientAsync(client, sessionId, stoppingToken);
            _clientTasks[sessionId] = task;
        }
        try
        {
            await pruneTask;
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("Prune task cancelled");
        }
    }
    private async Task PruneCompletedTasksAsync(CancellationToken ct)
    {
        while (await _pruneTimer.WaitForNextTickAsync(ct))
        {
            var completedKeys = _clientTasks.Where(kvp => kvp.Value.IsCompleted).Select(kvp => kvp.Key).ToList();
            foreach (var key in completedKeys)
            {
                _clientTasks.TryRemove(key, out _);
            }
            if (completedKeys.Count > 0 && _log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("Pruned {Count} completed client tasks", completedKeys.Count);
            }
        }
    }
    private async Task HandleClientAsync(TcpClient client, Guid sessionId, CancellationToken ct)
    {
        var session = new Session(client) { Id = sessionId };
        if (_log.IsEnabled(LogLevel.Information))
        {
            _log.LogInformation("Accepted new connection: {Session}", session);
        }
        _world.Add(session);
        using var _ = client;
        var writerTask = WriterLoopAsync(session);
        session.EnqueueWelcomeMessages();
        _world.Broadcast($"{Ansi.Dim}* A new presence tingles at the edge of reality...{Ansi.Reset}", except: session);
        using var reader = new StreamReader(session.Stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
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
                if (!_inbound.Writer.TryWrite(new InboundMessage(session, line)))
                {
                    _log.LogWarning("Dropped inbound message from {Session} (channel full)", session);
                }
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
        catch (SocketException)
        {
            _log.LogWarning("Socket error for {Session}", session);
        }
        finally
        {
            _world.Remove(session);
            _world.Broadcast($"{Ansi.Dim}* {session} has left the world.{Ansi.Reset}");
            session.OutboundWriter.TryComplete();
            if (_log.IsEnabled(LogLevel.Information))
            {
                _log.LogInformation("Disconnected {Session}", session);
            }
        }
        await writerTask;
        _clientTasks.TryRemove(sessionId, out var _);
    }
    private static async Task WriterLoopAsync(Session session)
    {
        using var writer = new StreamWriter(session.Stream, new UTF8Encoding(false))
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        await foreach (var msg in session.OutboundReader.ReadAllAsync())
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
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _listener.Stop();
        if (_log.IsEnabled(LogLevel.Information))
        {
            _log.LogInformation("Waiting for {Count} active client connections to complete...", _clientTasks.Count);
        }
        _world.CloseAllSessions();
        var activeTasks = _clientTasks.Values.ToArray();
        if (activeTasks.Length > 0)
        {
            var timeout = Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);
            var allTasks = Task.WhenAll(activeTasks);
            var completed = await Task.WhenAny(allTasks, timeout);
            if (completed == timeout)
            {
                _log.LogWarning("Grace period elapsed. {Count} tasks still active. Proceeding with shutdown.", activeTasks.Count(t => !t.IsCompleted));
            }
            else
            {
                _log.LogInformation("All client connections completed gracefully.");
            }
        }
        await base.StopAsync(cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }
    public override void Dispose()
    {
        _listener.Stop();
        _listener.Dispose();
        _pruneTimer.Dispose();
        base.Dispose();
    }
}
