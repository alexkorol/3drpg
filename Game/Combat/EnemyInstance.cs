using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rpg3D.Game.Combat;

/// <summary>
/// Runtime state for a single enemy billboard in the scene.
/// </summary>
public sealed class EnemyInstance
{
    private float _health;

    public Vector3 Origin { get; set; }

    public Vector3 Position { get; set; }

    public Texture2D? Sprite { get; set; }

    public Vector2 SpriteSize { get; set; }

    public Color BaseTint { get; set; }

    public Color GlowColor { get; set; }

    public float OrbitRadius { get; set; }

    public float OrbitSpeed { get; set; }

    public float BobHeight { get; set; }

    public float BobSpeed { get; set; }

    public float Phase { get; set; }

    public float MaxHealth { get; set; } = 10f;

    public float HitFlashTimer { get; set; }

    public float Health
    {
        get => _health;
        set => _health = Math.Clamp(value, 0f, MaxHealth);
    }

    public bool IsDead => Health <= 0f;

    public void ResetFlash(float durationSeconds)
    {
        HitFlashTimer = Math.Max(HitFlashTimer, durationSeconds);
    }

    public Color GetCurrentTint()
    {
        if (HitFlashTimer <= 0f)
        {
            return BaseTint;
        }

        var flashStrength = MathHelper.Clamp(HitFlashTimer * 8f, 0f, 1f);
        var flashColor = Color.Lerp(BaseTint, Color.White, flashStrength * 0.75f);
        return flashColor;
    }
}
