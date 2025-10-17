using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheFracturedRealm.Features;

namespace TheFracturedRealm;

public sealed class GameLoopService : BackgroundService
{
    private readonly ILogger<GameLoopService> _log;
    private readonly Channel<InboundMessage> _inbound;
    private readonly World _world;
    private readonly CommandDispatcher _dispatcher = new();
    public GameLoopService(ILogger<GameLoopService> log, Channel<InboundMessage> inbound, World world)
    {
        _log = log;
        _inbound = inbound;
        _world = world;
        _dispatcher.Register(new NameCommand());
        _dispatcher.Register(new SayCommand());
        _dispatcher.Register(new LookCommand());
        _dispatcher.Register(new WhoCommand());
        _dispatcher.Register(new HelpCommand(_dispatcher));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tick = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (!stoppingToken.IsCancellationRequested)
        {
            while (_inbound.Reader.TryRead(out var inbound))
            {
                await HandleInbound(inbound, stoppingToken);
            }
            await tick.WaitForNextTickAsync(stoppingToken);
        }
    }
    private async Task HandleInbound(InboundMessage msg, CancellationToken ct)
    {
        try
        {
            if (!await _dispatcher.TryDispatchAsync(msg, _world, ct))
            {
                if (!IsNamed(msg.Session))
                {
                    await Tell(msg.Session, $"Please set your handle with: {Ansi.Yellow}name <yourname>{Ansi.Reset}");
                    return;
                }
                var say = new SayCommand();
                await say.ExecuteAsync(new CommandContext(msg, _world), msg.Line, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error handling input");
            await Tell(msg.Session, $"{Ansi.Red}Oops. Something went wrong processing that.{Ansi.Reset}");
        }
    }
    private static bool IsNamed(Session s) => !string.IsNullOrWhiteSpace(s.Name);
    private static Task Tell(Session s, string line)
    {
        s.OutboundWriter.TryWrite(new OutboundMessage(line));
        return Task.CompletedTask;
    }
}
