using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheFracturedRealm;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((services) =>
{
    services.AddSingleton(Channel.CreateBounded<InboundMessage>(new BoundedChannelOptions(2000)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest
    }));
    services.AddSingleton<World>();
    services.AddHostedService<TcpServerService>();
    services.AddHostedService<GameLoopService>();
});
await builder.Build().RunAsync();
