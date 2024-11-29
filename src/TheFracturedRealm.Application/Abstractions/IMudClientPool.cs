using TheFracturedRealm.Domain;

namespace TheFracturedRealm.Application.Abstractions;

public interface IMudClientPool
{
    bool TryAddClient(MudClient client);
    bool TryRemoveClient(Guid clientId);
    MudClient? GetClient(Guid clientId);
    IReadOnlyCollection<MudClient> GetAllClients();
    Task NotifyAllAsync(string message);
}
