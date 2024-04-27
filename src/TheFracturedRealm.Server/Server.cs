using System.Net.Sockets;

namespace TheFracturedRealm.Server;

public sealed class Server
{
    private readonly TcpListener _listener;
    public Server(TcpListener listener)
    {
        _listener = listener;
    }

    public async Task RunAsync()
    {
        _listener.Start();
        Console.WriteLine("Server started on port 4000.");
        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            await ConnectionHandler.HandleConnectionAsync(client);
        }
    }
}
