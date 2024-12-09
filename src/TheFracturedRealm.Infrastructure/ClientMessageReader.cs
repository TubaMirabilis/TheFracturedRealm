using System.Text;
using TheFracturedRealm.Application.Abstractions;

namespace TheFracturedRealm.Infrastructure;

public sealed class ClientMessageReader : IClientMessageReader
{
    public async IAsyncEnumerable<string> ReadMessagesAsync(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var readBuffer = new byte[1024];
        var messageBuffer = new List<byte>();
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), ct)) != 0)
        {
            messageBuffer.AddRange(readBuffer.AsSpan(0, bytesRead));
            int delimiterIndex;
            while ((delimiterIndex = messageBuffer.IndexOf((byte)'\n')) != -1)
            {
                var lineBytes = messageBuffer.Take(delimiterIndex).ToArray();
                messageBuffer.RemoveRange(0, delimiterIndex + 1);
                var message = Encoding.UTF8.GetString(lineBytes).TrimEnd('\r');
                yield return message;
            }
        }
    }
}
