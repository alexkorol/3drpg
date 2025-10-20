using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rpg3D.Engine.Core;
using Rpg3D.Engine.Graphics;
using Rpg3D.Engine.Input;
using Rpg3D.Engine.World;

namespace Rpg3D.Game.Systems;

public sealed class PlayerControllerSystem : IUpdateSystem
{
    private const float MoveSpeed = 4.5f;
    private const float MouseSensitivity = 0.0025f;
    private const float PlayerRadius = 0.3f;
    private const float CellSize = 1f;
    private const float EyeHeight = 1.65f;
    private const float BobFrequency = 7.5f;

    private float _bobTime;
    private float _bobOffset;
    private float _targetBobOffset;

    private InputService? _input;
    private Camera3D? _camera;
    private GridMap? _map;
    private Vector3 _spawnPosition = new(2.5f, EyeHeight, -2.5f);

    public void Initialize(ServiceRegistry services)
    {
        _input = services.Require<InputService>();
        _camera = services.Require<Camera3D>();

        if (_camera != null)
        {
            _camera.Position = _spawnPosition;
            _camera.RecalculateProjection();
        }

        if (_input != null)
        {
            _input.CaptureMouse = true;
        }
    }

    public void Update(GameClock clock)
    {
        if (_input == null || _camera == null)
        {
            return;
        }

        var snapshot = _input.Snapshot;

        if (snapshot.WasKeyPressed(Keys.Tab))
        {
            _input.CaptureMouse = !_input.CaptureMouse;
        }

        if (!_input.CaptureMouse)
        {
            return;
        }

        ApplyLook(snapshot);
        ApplyMovement(snapshot, clock.DeltaTime);
        UpdateHeadBob(clock.DeltaTime, snapshot);
        ApplyHeadBob();
    }

    public void SetMap(GridMap map)
    {
        _map = map;
    }

    public void SetSpawnPoint(Vector3 worldPosition)
    {
        _spawnPosition = new Vector3(worldPosition.X, EyeHeight, worldPosition.Z);
        if (_camera != null)
        {
            _camera.Position = _spawnPosition;
        }
    }

    private void ApplyLook(InputSnapshot snapshot)
    {
        if (_camera == null)
        {
            return;
        }

        var delta = snapshot.MouseDelta;
        if (delta == Point.Zero)
        {
            return;
        }

        var yawChange = -delta.X * MouseSensitivity;
        var pitchChange = -delta.Y * MouseSensitivity;
        _camera.ApplyRotation(yawChange, pitchChange);
    }

    private void ApplyMovement(InputSnapshot snapshot, float deltaSeconds)
    {
        if (_camera == null)
        {
            return;
        }

        var forward = Flatten(_camera.Forward);
        var right = Flatten(_camera.Right);

        var move = Vector3.Zero;

        if (snapshot.IsKeyDown(Keys.W))
        {
            move += forward;
        }

        if (snapshot.IsKeyDown(Keys.S))
        {
            move -= forward;
        }

        if (snapshot.IsKeyDown(Keys.D))
        {
            move += right;
        }

        if (snapshot.IsKeyDown(Keys.A))
        {
            move -= right;
        }

        if (move == Vector3.Zero)
        {
            _targetBobOffset = 0f;
            return;
        }

        move.Normalize();
        var displacement = move * MoveSpeed * deltaSeconds;
        MoveWithCollisions(displacement);
        _targetBobOffset = 1f;
    }

    private static Vector3 Flatten(Vector3 vector)
    {
        vector.Y = 0f;
        return vector.LengthSquared() > 0f ? Vector3.Normalize(vector) : Vector3.Zero;
    }

    private void MoveWithCollisions(Vector3 displacement)
    {
        if (_camera == null)
        {
            return;
        }

        var position = _camera.Position;

        var horizontalMoveX = new Vector3(displacement.X, 0f, 0f);
        if (!WillCollide(position + horizontalMoveX))
        {
            position += horizontalMoveX;
        }

        var horizontalMoveZ = new Vector3(0f, 0f, displacement.Z);
        if (!WillCollide(position + horizontalMoveZ))
        {
            position += horizontalMoveZ;
        }

        position.Y = EyeHeight;
        _camera.Position = position;
    }

    private bool WillCollide(Vector3 position)
    {
        if (_map == null)
        {
            return false;
        }

        Span<Vector2> offsets = stackalloc Vector2[]
        {
            new(-PlayerRadius, -PlayerRadius),
            new(-PlayerRadius, PlayerRadius),
            new(PlayerRadius, -PlayerRadius),
            new(PlayerRadius, PlayerRadius),
            Vector2.Zero
        };

        foreach (var offset in offsets)
        {
            var sample = new Vector3(position.X + offset.X, 0f, position.Z + offset.Y);
            var cellX = (int)MathF.Floor(sample.X / CellSize);
            var cellY = (int)MathF.Floor(-sample.Z / CellSize);

            if (_map.IsBlocked(cellX, cellY))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateHeadBob(float deltaSeconds, InputSnapshot snapshot)
    {
        var moveInput =
            snapshot.IsKeyDown(Keys.W) ||
            snapshot.IsKeyDown(Keys.A) ||
            snapshot.IsKeyDown(Keys.S) ||
            snapshot.IsKeyDown(Keys.D);

        if (!moveInput)
        {
            _targetBobOffset = 0f;
        }

        _bobTime += deltaSeconds * BobFrequency * MathF.Max(0.2f, _targetBobOffset);
        var bob = MathF.Sin(_bobTime) * 0.08f * _targetBobOffset;
        _bobOffset = MathHelper.Lerp(_bobOffset, bob, MathF.Min(1f, deltaSeconds * 10f));
    }

    private void ApplyHeadBob()
    {
        if (_camera == null)
        {
            return;
        }

        var position = _camera.Position;
        position.Y = EyeHeight + _bobOffset;
        _camera.Position = position;
    }
}
