using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm;

public sealed class CommandDispatcher
{
    private readonly List<ICommand> _cmds = new();
    public void Register(ICommand cmd) => _cmds.Add(cmd);
    public async Task<bool> TryDispatchAsync(InboundMessage msg, World world, CancellationToken ct)
    {
        var line = msg.Line;
        foreach (var cmd in _cmds)
        {
            if (cmd.Matches(line))
            {
                await cmd.ExecuteAsync(new CommandContext(msg, world), line, ct);
                return true;
            }
        }
        return false;
    }
    public IEnumerable<ICommand> Commands => _cmds;
}
