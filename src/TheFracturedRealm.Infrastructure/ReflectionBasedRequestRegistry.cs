using System.Reflection;
using Mediator;
using TheFracturedRealm.Application.Abstractions;
using TheFracturedRealm.Application.Attributes;

namespace TheFracturedRealm.Infrastructure;

public sealed class ReflectionBasedRequestRegistry : IRequestRegistry
{
    private readonly Dictionary<string, Func<string, IRequest>> _factories = [];

    public ReflectionBasedRequestRegistry(Assembly assembly)
    {
        var requestTypes = assembly.DefinedTypes
            .Where(t => t.ImplementedInterfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToList();
        foreach (var t in requestTypes)
        {
            var aliasAttr = t.GetCustomAttribute<CommandAliasAttribute>();
            if (aliasAttr != null)
            {
                var ctor = t.GetConstructor([typeof(string)]);
                if (ctor != null)
                {
                    _factories[aliasAttr.Alias] = args => (IRequest)Activator.CreateInstance(t.AsType(), args)!;
                }
            }
        }
    }
    public bool TryCreateRequest(string requestAlias, string args, out IRequest request)
    {
        if (_factories.TryGetValue(requestAlias, out var factory))
        {
            request = factory(args);
            return true;
        }

        request = null!;
        return false;
    }
}
