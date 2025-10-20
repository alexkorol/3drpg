using System;
using System.Collections.Concurrent;

namespace Rpg3D.Engine.Core;

/// <summary>
/// Minimal service locator for sharing engine-wide dependencies.
/// </summary>
public sealed class ServiceRegistry
{
    private readonly ConcurrentDictionary<Type, object> _services = new();

    public void Register<TService>(TService instance) where TService : class
    {
        if (instance is null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        _services[typeof(TService)] = instance;
    }

    public bool TryGet<TService>(out TService? result) where TService : class
    {
        if (_services.TryGetValue(typeof(TService), out var instance) && instance is TService typed)
        {
            result = typed;
            return true;
        }

        result = null;
        return false;
    }

    public TService Require<TService>() where TService : class
    {
        if (!TryGet<TService>(out var result) || result is null)
        {
            throw new InvalidOperationException($"Required service '{typeof(TService).Name}' has not been registered.");
        }

        return result;
    }
}
