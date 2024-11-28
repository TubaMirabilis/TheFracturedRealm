namespace TheFracturedRealm.Server;

internal interface IMudClientPool
{
    bool TryAddClient(MudClient client);
    bool TryRemoveClient(Guid clientId);
    MudClient? GetClient(Guid clientId);
    IReadOnlyCollection<MudClient> GetAllClients();
}
