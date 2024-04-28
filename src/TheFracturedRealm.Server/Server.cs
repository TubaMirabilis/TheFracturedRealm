using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TheFracturedRealm.Server;

public static class Server
{
    private static readonly List<MudClient> _clients = [];
    public static IReadOnlyList<MudClient> Clients => _clients.AsReadOnly();
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
            _clients.Add(mudClient);
            await ConnectionHandler.HandleConnectionAsync(mudClient);
        }
    }
    public static void RemoveClient(MudClient client) => _clients.Remove(client);
    public static void BroadcastMessage(MudClient sender, string message)
    {
        var clients = new ConcurrentBag<MudClient>(_clients);
        Parallel.ForEach(clients, async client =>
        {
            if (client != sender)
            {
                await client.NotifyAsync(message);
            }
        });
    }
}
