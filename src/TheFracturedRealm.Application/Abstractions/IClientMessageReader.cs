namespace TheFracturedRealm.Application.Abstractions;

public interface IClientMessageReader
{
    IAsyncEnumerable<string> ReadMessagesAsync(Stream stream, CancellationToken ct);
}
