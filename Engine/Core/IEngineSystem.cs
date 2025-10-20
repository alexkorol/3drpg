namespace Rpg3D.Engine.Core;

public interface IEngineSystem
{
    void Initialize(ServiceRegistry services);
}

public interface IUpdateSystem : IEngineSystem
{
    void Update(GameClock clock);
}

public interface IDrawSystem : IEngineSystem
{
    void Draw(GameClock clock);
}
