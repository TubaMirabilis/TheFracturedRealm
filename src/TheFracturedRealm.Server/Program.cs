using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TheFracturedRealm.Server;

using var listener = new TcpListener(IPAddress.Any, 4000);
var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();
var services = new ServiceCollection();
services.AddSingleton<ILogger>(logger);
services.AddSingleton<TcpListener>(listener);
services.AddSingleton<Server>();
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
var serviceProvider = services.BuildServiceProvider();
var server = serviceProvider.GetRequiredService<Server>();
await server.RunAsync();
