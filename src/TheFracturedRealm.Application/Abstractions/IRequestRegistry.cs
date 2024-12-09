using Mediator;

namespace TheFracturedRealm.Application.Abstractions;

public interface IRequestRegistry
{
    bool TryCreateRequest(string requestAlias, string args, out IRequest request);
}
