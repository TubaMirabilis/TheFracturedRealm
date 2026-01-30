using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheFracturedRealm.FunctionalTests;

public sealed class RealmHostFixture : IAsyncLifetime
{
    public IHost Host { get; private set; } = default!;
    public async ValueTask InitializeAsync()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning)).ConfigureServices(services =>
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
        }).Build();
        await Host.StartAsync();
    }
    public async ValueTask DisposeAsync()
    {
        await Host.StopAsync();
        Host.Dispose();
    }
}
