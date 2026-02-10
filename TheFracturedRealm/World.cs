using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TheFracturedRealm;

internal sealed class World
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();
    private readonly ILogger<World> _log;
    public World(ILogger<World> log) => _log = log;
    public Session[] SnapshotSessions() => [.. _sessions.Values];
    public bool Add(Session s) => _sessions.TryAdd(s.Id, s);
    public bool Remove(Session s) => _sessions.TryRemove(s.Id, out _);
    public void CloseAllSessions()
    {
        var sessions = SnapshotSessions();
        if (_log.IsEnabled(LogLevel.Information))
        {
            _log.LogInformation("Forcefully closing {Count} active sessions", sessions.Length);
        }
        foreach (var session in sessions)
        {
            try
            {
                session.Close();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error closing session {Session}", session);
            }
        }
    }
    public void Broadcast(string line, Session? except = null)
    {
        if (_log.IsEnabled(LogLevel.Information))
        {
            _log.LogInformation("Broadcasting message to all sessions (except {Except}): {Line}", except, line);
        }
        foreach (var session in _sessions.Values)
        {
            if (except is not null && session.Id == except.Id)
            {
                continue;
            }
            if (!session.OutboundWriter.TryWrite(new OutboundMessage(line)))
            {
                _log.LogWarning("Dropped broadcast message to {Session} (channel full)", session);
            }
        }
    }
}
