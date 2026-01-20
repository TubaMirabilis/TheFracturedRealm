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
    public void Broadcast(string line, Session? except = null)
    {
        foreach (var session in _sessions.Values)
        {
            if (except is not null && session.Id == except.Id)
            {
                continue;
            }
            _ = session.OutboundWriter.TryWrite(new OutboundMessage(line));
        }
    }
}
