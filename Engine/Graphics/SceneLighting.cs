using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rpg3D.Engine.Graphics;

public sealed class SceneLighting
{
    public Vector3 MainLightDirection { get; set; } = Vector3.Normalize(new Vector3(-0.6f, -1.2f, 0.4f));

    public Color MainLightColor { get; set; } = new(190, 170, 140);

    public Color AmbientColor { get; set; } = new(40, 40, 60);

    public Color FogColor { get; set; } = new(10, 8, 15);

    public float FogStart { get; set; } = 6f;

    public float FogEnd { get; set; } = 30f;

    public List<PointLight> PointLights { get; } = new();
}
