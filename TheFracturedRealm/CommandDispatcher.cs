using TheFracturedRealm.Abstractions;
using TheFracturedRealm.Features;

namespace TheFracturedRealm;

internal sealed class CommandDispatcher
{
    private readonly List<ICommand> _cmds = [];
    private readonly ICommand? _fallback;
    public CommandDispatcher()
    {
        RegisterExistingCommands();
        _fallback = _cmds.FirstOrDefault(c => c is SayCommand);
    }
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
        if (_fallback is null)
        {
            return false;
        }
        await _fallback.ExecuteAsync(ctx, input, ct);
        return true;
    }
    public void RegisterExistingCommands()
    {
        Register(new NameCommand());
        Register(new LookCommand());
        Register(new WhoCommand());
        Register(new SayCommand());
        Register(new TellCommand());
        Register(new HelpCommand(() => Commands));
    }
    public IEnumerable<ICommand> Commands => _cmds;
}
