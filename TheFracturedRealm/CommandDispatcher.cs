using TheFracturedRealm.Abstractions;

namespace TheFracturedRealm;

internal sealed class CommandDispatcher
{
    private readonly List<ICommand> _cmds = [];
    public ICommand? Fallback { get; set; }
    public void Register(ICommand cmd) => _cmds.Add(cmd);
    public async Task<bool> TryDispatchAsync(InboundMessage msg, World world, CancellationToken ct)
    {
        var input = CommandInput.Parse(msg.Line);
        var cmd = _cmds.FirstOrDefault(c => c.Matches(input));
        var ctx = new CommandContext(msg, world);
        if (cmd is not null)
        {
            await cmd.ExecuteAsync(ctx, input, ct);
            return true;
        }
        if (Fallback is null)
        {
            return false;
        }
        await Fallback.ExecuteAsync(ctx, input, ct);
        return true;
    }
    public IEnumerable<ICommand> Commands => _cmds;
}
