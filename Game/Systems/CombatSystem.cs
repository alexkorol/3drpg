using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Graphics;
using Rpg3D.Engine.Input;
using Rpg3D.Game.Combat;

namespace Rpg3D.Game.Systems;

public sealed class CombatSystem : IUpdateSystem
{
    private const float AttackRange = 1.5f;
    private const float AttackRadius = 0.55f;
    private const float AttackDamage = 4f;
    private const float AttackCooldownSeconds = 0.42f;
    private const float AttackSwingSeconds = 0.32f;
    private const float AttackHitWindowStart = 0.12f;
    private const float AttackHitWindowEnd = 0.22f;
    private const float HitFlashDuration = 0.18f;

    private InputService? _input;
    private Camera3D? _camera;
    private EnemyRoster? _enemyRoster;

    private float _cooldownTimer;
    private float _swingElapsed;
    private bool _attackActive;
    private bool _attackWindowConsumed;
    private float _hitFlashTimer;
    private EnemyInstance? _lastHitEnemy;

    public bool IsSwinging => _attackActive;

    public float SwingProgress => _attackActive
        ? MathHelper.Clamp(_swingElapsed / AttackSwingSeconds, 0f, 1f)
        : 0f;

    public float HitFlashTimer => _hitFlashTimer;

    public float HitFlashStrength => HitFlashDuration <= 0f
        ? 0f
        : MathHelper.Clamp(_hitFlashTimer / HitFlashDuration, 0f, 1f);

    public float AttackSwingStrength
    {
        get
        {
            if (!_attackActive)
            {
                return 0f;
            }

            var progress = MathHelper.Clamp(_swingElapsed / AttackSwingSeconds, 0f, 1f);
            return MathF.Sin(progress * MathF.PI);
        }
    }

    public EnemyInstance? LastHitEnemy => _lastHitEnemy;

    public void Initialize(ServiceRegistry services)
    {
        _input = services.Require<InputService>();
        _camera = services.Require<Camera3D>();
        _enemyRoster = services.Require<EnemyRoster>();
    }

    public void Update(GameClock clock)
    {
        var dt = clock.DeltaTime;

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer = MathF.Max(0f, _cooldownTimer - dt);
        }

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer = MathF.Max(0f, _hitFlashTimer - dt);
        }

        if (!_attackActive)
        {
            TryBeginAttack();
            return;
        }

        _swingElapsed += dt;

        if (!_attackWindowConsumed)
        {
            if (_swingElapsed >= AttackHitWindowStart && _swingElapsed <= AttackHitWindowEnd)
            {
                if (TryPerformStrike())
                {
                    _attackWindowConsumed = true;
                }
            }
            else if (_swingElapsed > AttackHitWindowEnd)
            {
                _attackWindowConsumed = true;
            }
        }

        if (_swingElapsed >= AttackSwingSeconds)
        {
            _attackActive = false;
            _swingElapsed = 0f;
        }
    }

    private void TryBeginAttack()
    {
        if (_input == null || _camera == null || _enemyRoster == null)
        {
            return;
        }

        if (_cooldownTimer > 0f)
        {
            return;
        }

        if (!_input.CaptureMouse)
        {
            return;
        }

        var snapshot = _input.Snapshot;
        var leftPressed = snapshot.CurrentMouse.LeftButton == ButtonState.Pressed &&
                          snapshot.PreviousMouse.LeftButton == ButtonState.Released;

        if (!leftPressed)
        {
            return;
        }

        _attackActive = true;
        _attackWindowConsumed = false;
        _swingElapsed = 0f;
        _cooldownTimer = AttackCooldownSeconds;
        _lastHitEnemy = null;
    }

    private bool TryPerformStrike()
    {
        if (_camera == null || _enemyRoster == null)
        {
            return false;
        }

        var direction = _camera.Forward;
        if (direction.LengthSquared() <= 1e-6f)
        {
            return false;
        }

        direction.Normalize();

        var origin = _camera.Position + direction * 0.2f;

        var enemy = _enemyRoster.FindFirstHit(origin, direction, AttackRange, AttackRadius, out _);
        if (enemy == null)
        {
            return false;
        }

        enemy.Health = enemy.Health - AttackDamage;
        enemy.ResetFlash(HitFlashDuration);
        _lastHitEnemy = enemy;
        _hitFlashTimer = HitFlashDuration;
        return true;
    }
}
