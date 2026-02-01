using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheFracturedRealm;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((services) =>
{
    services.AddSingleton(Channel.CreateUnbounded<InboundMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    }));
    services.AddSingleton<World>();
    services.AddHostedService<TcpServerService>();
    services.AddHostedService<GameLoopService>();
});
await builder.Build().RunAsync();
