using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TheFracturedRealm.Application.Abstractions;
using TheFracturedRealm.Application.Abstractions.Data;
using TheFracturedRealm.Infrastructure;
using TheFracturedRealm.Server;

using var listener = new TcpListener(IPAddress.Any, 4000);
var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();
var connectionString = Environment.GetEnvironmentVariable("TFR_DATABASE__CONNECTIONSTRING") ?? throw new InvalidOperationException("Database connection string not found.");
var services = new ServiceCollection();
services.AddDbContext<IApplicationReadDbContext, ApplicationReadDbContext>(
    options => options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
services.AddDbContext<ApplicationWriteDbContext>(
    (sp, options) => options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationWriteDbContext>());
services.AddSingleton<IRequestRegistry>(new ReflectionBasedRequestRegistry(typeof(IRequestRegistry).Assembly));
services.AddScoped<IClientRequestProcessor, ClientRequestProcessor>();
services.AddScoped<IClientMessageReader, ClientMessageReader>();
services.AddSingleton<ILogger>(logger);
services.AddSingleton<TcpListener>(listener);
services.AddSingleton<Server>();
services.AddSingleton<IClientConnectionHandler, ClientConnectionHandler>();
services.AddSingleton<IMudClientPool, MudClientPool>();
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
var serviceProvider = services.BuildServiceProvider();
var server = serviceProvider.GetRequiredService<Server>();
await server.RunAsync(CancellationToken.None);
