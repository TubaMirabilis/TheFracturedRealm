namespace TheFracturedRealm;

internal sealed record CommandContext(InboundMessage Inbound, World World)
{
    public Session Session => Inbound.Session;
    public Task Reply(string text, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }
        Session.OutboundWriter.TryWrite(new OutboundMessage(text));
        return Task.CompletedTask;
    }
    public Task Broadcast(string text, Session? except = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }
        World.Broadcast(text, except);
        return Task.CompletedTask;
    }
}
