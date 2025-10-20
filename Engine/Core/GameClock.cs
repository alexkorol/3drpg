using Microsoft.Xna.Framework;

namespace Rpg3D.Engine.Core;

/// <summary>
/// Wraps MonoGame's GameTime into a simple snapshot that exposes useful timing data.
/// </summary>
public readonly struct GameClock
{
    public GameClock(GameTime gameTime)
    {
        Raw = gameTime;
        TotalTime = gameTime.TotalGameTime.TotalSeconds;
        DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public GameTime Raw { get; }

    public double TotalTime { get; }

    public float DeltaTime { get; }
}
