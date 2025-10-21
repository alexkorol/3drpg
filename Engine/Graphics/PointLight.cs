using Microsoft.Xna.Framework;

namespace Rpg3D.Engine.Graphics;

public readonly struct PointLight
{
    public PointLight(Vector3 position, Color color, float radius, float intensity = 1f)
    {
        Position = position;
        Color = color;
        Radius = radius;
        Intensity = intensity;
    }

    public Vector3 Position { get; }

    public Color Color { get; }

    public float Radius { get; }

    public float Intensity { get; }
}
