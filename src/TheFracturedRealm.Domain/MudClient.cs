using System.Net.Sockets;
using System.Text;

namespace TheFracturedRealm.Domain;

public sealed class MudClient : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private volatile bool _isDisposed;
    public MudClient(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        Id = Guid.NewGuid();
    }
    public Guid Id { get; private set; }
    public Character? Character { get; set; }
    public NetworkStream GetStream() => _tcpClient.GetStream();
    public async Task NotifyAsync(string message)
    {
        if (_isDisposed)
        {
            return;
        }
        await _writeSemaphore.WaitAsync();
        try
        {
            if (_isDisposed)
            {
                return;
            }
            var stream = GetStream();
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(messageBytes);
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }
    public void Dispose()
    {
        _isDisposed = true;
        _tcpClient.Dispose();
        _writeSemaphore.Dispose();
    }
}
