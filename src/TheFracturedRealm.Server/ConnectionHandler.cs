using System.Net.Sockets;
using System.Text;

namespace TheFracturedRealm.Server;

public static class ConnectionHandler
{
    public static async Task HandleConnectionAsync(TcpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var stream = client.GetStream();
        var buffer = new byte[1024];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
        {
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Received: " + request);
            var response = Encoding.UTF8.GetBytes("Echo: " + request);
            await stream.WriteAsync(response);
        }
    }
}
