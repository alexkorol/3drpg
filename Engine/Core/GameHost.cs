using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rpg3D.Engine.Core;

/// <summary>
/// Coordinates systems and shared services for the game.
/// </summary>
public sealed class GameHost
{
    private readonly List<IEngineSystem> _allSystems = new();
    private readonly List<IUpdateSystem> _updateSystems = new();
    private readonly List<IDrawSystem> _drawSystems = new();
    private bool _initialized;

    public ServiceRegistry Services { get; } = new();

    public void RegisterService<TService>(TService instance) where TService : class
    {
        Services.Register(instance);
    }

    public void AddSystem(IEngineSystem system)
    {
        _allSystems.Add(system);

        if (system is IUpdateSystem updateSystem)
        {
            _updateSystems.Add(updateSystem);
        }

        if (system is IDrawSystem drawSystem)
        {
            _drawSystems.Add(drawSystem);
        }
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        foreach (var system in _allSystems)
        {
            system.Initialize(Services);
        }

        _initialized = true;
    }

    public void Update(GameTime gameTime)
    {
        var clock = new GameClock(gameTime);
        foreach (var system in _updateSystems)
        {
            system.Update(clock);
        }
    }

    public void Draw(GameTime gameTime)
    {
        var clock = new GameClock(gameTime);
        foreach (var system in _drawSystems)
        {
            system.Draw(clock);
        }
    }
}
