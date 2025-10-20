using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Rendering;

namespace Rpg3D.Game.Systems;

public sealed class ParticleSystem : IUpdateSystem
{
    private readonly List<Particle> _particles = new();
    private readonly List<ParticleEmitter> _emitters = new();
    private BillboardRenderer? _billboardRenderer;
    private readonly Random _random = new();

    public void Initialize(ServiceRegistry services)
    {
        _billboardRenderer = services.Require<BillboardRenderer>();
    }

    public void Update(GameClock clock)
    {
        var dt = clock.DeltaTime;

        foreach (var emitter in _emitters)
        {
            emitter.TimeAccumulator += dt;
            var spawnInterval = 1f / Math.Max(0.01f, emitter.SpawnRate);

            while (emitter.TimeAccumulator >= spawnInterval)
            {
                emitter.TimeAccumulator -= spawnInterval;
                SpawnParticle(emitter);
            }
        }

        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Age += dt;
            if (particle.Age >= particle.Lifetime)
            {
                _particles.RemoveAt(i);
                continue;
            }

            particle.Position += particle.Velocity * dt;
            var lifeT = particle.Age / particle.Lifetime;
            var size = MathHelper.Lerp(particle.StartSize, particle.EndSize, lifeT);
            var color = Color.Lerp(particle.StartColor, particle.EndColor, lifeT);
            _particles[i] = particle;

            _billboardRenderer?.Submit(
                new BillboardInstance(
                    particle.Position,
                    new Vector2(size),
                    color,
                    particle.Texture,
                    particle.Additive));
        }
    }

    public ParticleEmitter AddEmitter(ParticleEmitter emitter)
    {
        _emitters.Add(emitter);
        return emitter;
    }

    public void ClearEmitters()
    {
        _emitters.Clear();
        _particles.Clear();
    }

    private void SpawnParticle(ParticleEmitter emitter)
    {
        var direction = emitter.Direction;
        if (emitter.RandomizeDirection)
        {
            var jitter = new Vector3(
                GetRandomRange(-emitter.DirectionJitter, emitter.DirectionJitter),
                GetRandomRange(-emitter.DirectionJitter * 0.5f, emitter.DirectionJitter * 0.5f),
                GetRandomRange(-emitter.DirectionJitter, emitter.DirectionJitter));
            direction = Vector3.Normalize(direction + jitter);
        }

        var velocity = direction * GetRandomRange(emitter.MinSpeed, emitter.MaxSpeed);

        var particle = new Particle
        {
            Position = emitter.Position,
            Velocity = velocity,
            Lifetime = GetRandomRange(emitter.MinLifetime, emitter.MaxLifetime),
            StartSize = emitter.StartSize,
            EndSize = emitter.EndSize,
            StartColor = emitter.StartColor,
            EndColor = emitter.EndColor,
            Additive = emitter.Additive,
            Texture = emitter.Texture
        };

        _particles.Add(particle);
    }

    private float GetRandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }

    private struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Lifetime;
        public float Age;
        public float StartSize;
        public float EndSize;
        public Color StartColor;
        public Color EndColor;
        public bool Additive;
        public Texture2D? Texture;
    }
}

public sealed class ParticleEmitter
{
    public Vector3 Position { get; set; }

    public Vector3 Direction { get; set; } = Vector3.Up;

    public bool RandomizeDirection { get; set; } = true;

    public float DirectionJitter { get; set; } = 0.4f;

    public float SpawnRate { get; set; } = 12f;

    public float MinSpeed { get; set; } = 0.2f;

    public float MaxSpeed { get; set; } = 0.8f;

    public float MinLifetime { get; set; } = 0.4f;

    public float MaxLifetime { get; set; } = 0.8f;

    public float StartSize { get; set; } = 0.25f;

    public float EndSize { get; set; } = 0.05f;

    public Color StartColor { get; set; } = new(255, 140, 64, 200);

    public Color EndColor { get; set; } = new(255, 64, 32, 0);

    public bool Additive { get; set; } = true;

    public Texture2D? Texture { get; set; }

    internal float TimeAccumulator;
}
