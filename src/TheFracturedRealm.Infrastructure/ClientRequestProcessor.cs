using System.Net.Sockets;
using System.Text;
using Mediator;
using Microsoft.Extensions.Logging;
using TheFracturedRealm.Application.Abstractions;

namespace TheFracturedRealm.Infrastructure;

public sealed class ClientRequestProcessor : IClientRequestProcessor
{
    private readonly IRequestRegistry _requestRegistry;
    private readonly ILogger<ClientRequestProcessor> _logger;
    private readonly IClientMessageReader _reader;
    private readonly ISender _sender;
    public ClientRequestProcessor(
        IRequestRegistry requestRegistry,
        ILogger<ClientRequestProcessor> logger,
        IClientMessageReader reader,
        ISender sender)
    {
        _requestRegistry = requestRegistry;
        _logger = logger;
        _reader = reader;
        _sender = sender;
    }
    public async Task ProcessRequestsAsync(NetworkStream stream, CancellationToken ct)
    {
        await foreach (var requestLine in _reader.ReadMessagesAsync(stream, ct))
        {
            _logger.LogInformation("Received: {Request}", requestLine);
            var parts = requestLine.Split(' ', 2);
            var alias = parts[0];
            var args = parts.Length > 1 ? parts[1] : string.Empty;
            if (!_requestRegistry.TryCreateRequest(alias, args, out var request))
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("Syntax Error.\n"), ct);
                continue;
            }
            await _sender.Send(request, ct);
        }
    }
}
