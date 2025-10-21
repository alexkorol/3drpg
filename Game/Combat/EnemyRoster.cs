using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rpg3D.Game.Combat;

/// <summary>
/// Simple container for tracking active enemies and providing basic queries.
/// </summary>
public sealed class EnemyRoster
{
    private readonly List<EnemyInstance> _enemies = new();

    public IReadOnlyList<EnemyInstance> Enemies => _enemies;

    public void Clear() => _enemies.Clear();

    public void Add(EnemyInstance enemy)
    {
        if (enemy == null)
        {
            throw new ArgumentNullException(nameof(enemy));
        }

        _enemies.Add(enemy);
    }

    public void RemoveDead(Action<EnemyInstance>? onRemoved = null)
    {
        for (var i = _enemies.Count - 1; i >= 0; i--)
        {
            if (!_enemies[i].IsDead)
            {
                continue;
            }

            var removed = _enemies[i];
            _enemies.RemoveAt(i);
            onRemoved?.Invoke(removed);
        }
    }

    public EnemyInstance? FindFirstHit(Vector3 origin, Vector3 direction, float maxDistance, float hitRadius, out float distance)
    {
        distance = float.MaxValue;
        EnemyInstance? result = null;

        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead)
            {
                continue;
            }

            var toEnemy = enemy.Position - origin;
            var projected = Vector3.Dot(toEnemy, direction);
            if (projected <= 0f || projected > maxDistance)
            {
                continue;
            }

            var closestPoint = origin + direction * projected;
            var separation = Vector3.Distance(enemy.Position, closestPoint);
            if (separation > hitRadius)
            {
                continue;
            }

            if (projected < distance)
            {
                distance = projected;
                result = enemy;
            }
        }

        return result;
    }
}
