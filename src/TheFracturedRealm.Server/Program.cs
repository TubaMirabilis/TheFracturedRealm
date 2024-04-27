using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using TheFracturedRealm.Server;

var services = new ServiceCollection();
ConfigureServices(services);
var serviceProvider = services.BuildServiceProvider();
var server = serviceProvider.GetRequiredService<Server>();
await server.RunAsync();

static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<TcpListener>(_ => new TcpListener(IPAddress.Any, 4000));
    services.AddSingleton<Server>();
}
