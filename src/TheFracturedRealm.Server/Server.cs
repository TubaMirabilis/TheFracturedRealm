using System.Net;
using System.Net.Sockets;

namespace TheFracturedRealm.Server;

public static class Server
{
    public static async Task RunAsync()
    {
        #pragma warning disable CA2000
        var listener = new TcpListener(IPAddress.Any, 4000);
        #pragma warning restore CA2000
        listener.Start();
        Console.WriteLine("Server started on port 4000.");
        while (true)
        {
            var tcpClient = await listener.AcceptTcpClientAsync();
            var mudClient = new MudClient(tcpClient);
            await ConnectionHandler.HandleConnectionAsync(mudClient);
        }
    }
}
