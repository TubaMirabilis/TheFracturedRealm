using System.Collections.Concurrent;

namespace TheFracturedRealm.Server;

internal sealed class MudClientPool : IMudClientPool
{
    private readonly ConcurrentDictionary<Guid, MudClient> _clients;
    public MudClientPool() => _clients = new ConcurrentDictionary<Guid, MudClient>();
    public bool TryAddClient(MudClient client) => _clients.TryAdd(client.Id, client);
    public bool TryRemoveClient(Guid clientId)
    {
        if (_clients.TryRemove(clientId, out var client))
        {
            client.Dispose();
            return true;
        }
        return false;
    }
    public MudClient? GetClient(Guid clientId) =>
        _clients.TryGetValue(clientId, out var client) ? client : null;
    public IReadOnlyCollection<MudClient> GetAllClients() => _clients.Values.ToList().AsReadOnly();
}
