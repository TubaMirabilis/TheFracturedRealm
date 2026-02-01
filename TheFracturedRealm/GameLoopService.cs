using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheFracturedRealm.Features;

namespace TheFracturedRealm;

internal sealed class GameLoopService : BackgroundService
{
    private readonly ILogger<GameLoopService> _log;
    private readonly Channel<InboundMessage> _inbound;
    private readonly World _world;
    private readonly CommandDispatcher _dispatcher;
    public GameLoopService(CommandDispatcher dispatcher, Channel<InboundMessage> inbound, ILogger<GameLoopService> log, World world)
    {
        _dispatcher = dispatcher;
        _inbound = inbound;
        _log = log;
        _world = world;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var reader = _inbound.Reader;
            while (await reader.WaitToReadAsync(stoppingToken))
            {
                while (reader.TryRead(out var inbound))
                {
                    await HandleInbound(inbound, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _log.LogInformation("Game loop stopping");
        }
    }
    private async Task HandleInbound(InboundMessage msg, CancellationToken ct)
    {
        try
        {
            if (!await _dispatcher.TryDispatchAsync(msg, _world, ct) && !IsNamed(msg.Session))
            {
                await Tell(msg.Session, $"Please set your handle with: {Ansi.Yellow}name <yourname>{Ansi.Reset}");
                return;
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
