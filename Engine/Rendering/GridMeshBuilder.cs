using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rpg3D.Engine.World;

namespace Rpg3D.Engine.Rendering;

public sealed class GridMesh
{
    public GridMesh(IReadOnlyList<GridMeshPartData> parts)
    {
        Parts = parts;
    }

    public IReadOnlyList<GridMeshPartData> Parts { get; }
}

public sealed class GridMeshPartData
{
    public GridMeshPartData(string textureKey, VertexPositionNormalTexture[] vertices, short[] indices)
    {
        TextureKey = textureKey;
        Vertices = vertices;
        Indices = indices;
    }

    public string TextureKey { get; }

    public VertexPositionNormalTexture[] Vertices { get; }

    public short[] Indices { get; }
}

public static class GridMeshBuilder
{
    private const string FloorTexture = "Textures/floor_tan";
    private const string CeilingTexture = "Textures/ceiling";
    private const string WallOrangeTexture = "Textures/orange_brick_wall";
    private const string WallMossyTexture = "Textures/mossy_brick_wall";

    public static GridMesh Build(GridMap map, float cellSize = 1f, float wallHeight = 2f, bool includeCeiling = true)
    {
        var builders = new Dictionary<string, MeshPartBuilder>();

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var cell = map[x, y];
                var basePosition = new Vector3(x * cellSize, 0f, -y * cellSize);

                if (cell != CellType.Wall)
                {
                    AddHorizontalQuad(
                        builders.GetOrCreate(FloorTexture),
                        basePosition,
                        cellSize,
                        Vector3.Up);

                    if (includeCeiling)
                    {
                        AddHorizontalQuad(
                            builders.GetOrCreate(CeilingTexture),
                            basePosition + new Vector3(0f, wallHeight, 0f),
                            cellSize,
                            Vector3.Down,
                            flipWinding: true);
                    }
                }

                if (cell == CellType.Wall)
                {
                    AddWallQuads(map, builders, x, y, basePosition, cellSize, wallHeight);
                }
            }
        }

        var parts = new List<GridMeshPartData>(builders.Count);
        foreach (var builder in builders.Values)
        {
            parts.Add(builder.ToMeshPart());
        }

        return new GridMesh(parts);
    }

    private static void AddHorizontalQuad(
        MeshPartBuilder builder,
        Vector3 origin,
        float size,
        Vector3 normal,
        bool flipWinding = false)
    {
        var p0 = origin;
        var p1 = origin + new Vector3(size, 0f, 0f);
        var p2 = origin + new Vector3(size, 0f, -size);
        var p3 = origin + new Vector3(0f, 0f, -size);

        var uv0 = new Vector2(0f, 0f);
        var uv1 = new Vector2(1f, 0f);
        var uv2 = new Vector2(1f, 1f);
        var uv3 = new Vector2(0f, 1f);

        builder.AddQuad(p0, p1, p2, p3, normal, uv0, uv1, uv2, uv3, flipWinding);
    }

    private static void AddWallQuads(
        GridMap map,
        Dictionary<string, MeshPartBuilder> builders,
        int cellX,
        int cellY,
        Vector3 origin,
        float size,
        float height)
    {
        var topOffset = new Vector3(0f, height, 0f);
        var verticalTiles = Math.Max(1f, height / size);

        var wallTexture = ((cellX * 73856093) ^ (cellY * 19349663)) % 100 < 50
            ? WallOrangeTexture
            : WallMossyTexture;
        var builder = builders.GetOrCreate(wallTexture);

        var west = map[cellX - 1, cellY];
        var east = map[cellX + 1, cellY];
        var north = map[cellX, cellY - 1];
        var south = map[cellX, cellY + 1];

        if (west != CellType.Wall)
        {
            var p0 = origin;
            var p1 = origin + new Vector3(0f, 0f, -size);
            var p2 = p1 + topOffset;
            var p3 = p0 + topOffset;
            builder.AddQuad(
                p0,
                p1,
                p2,
                p3,
                Vector3.Left,
                new Vector2(1f, verticalTiles),
                new Vector2(0f, verticalTiles),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f));
        }

        if (east != CellType.Wall)
        {
            var baseEast = origin + new Vector3(size, 0f, 0f);
            var p0 = baseEast;
            var p1 = baseEast + new Vector3(0f, 0f, -size);
            var p2 = p1 + topOffset;
            var p3 = p0 + topOffset;
            builder.AddQuad(
                p1,
                p0,
                p3,
                p2,
                Vector3.Right,
                new Vector2(1f, verticalTiles),
                new Vector2(0f, verticalTiles),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f));
        }

        if (north != CellType.Wall)
        {
            var baseNorth = origin + new Vector3(0f, 0f, -size);
            var p0 = baseNorth;
            var p1 = baseNorth + new Vector3(size, 0f, 0f);
            var p2 = p1 + topOffset;
            var p3 = p0 + topOffset;
            builder.AddQuad(
                p1,
                p0,
                p3,
                p2,
                Vector3.Backward,
                new Vector2(1f, verticalTiles),
                new Vector2(0f, verticalTiles),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f));
        }

        if (south != CellType.Wall)
        {
            var p0 = origin;
            var p1 = origin + new Vector3(size, 0f, 0f);
            var p2 = p1 + topOffset;
            var p3 = p0 + topOffset;
            builder.AddQuad(
                p0,
                p1,
                p2,
                p3,
                Vector3.Forward,
                new Vector2(1f, verticalTiles),
                new Vector2(0f, verticalTiles),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f));
        }
    }

    private sealed class MeshPartBuilder
    {
        private readonly string _textureKey;
        private readonly List<VertexPositionNormalTexture> _vertices = new();
        private readonly List<short> _indices = new();

        public MeshPartBuilder(string textureKey)
        {
            _textureKey = textureKey;
        }

        public MeshPartBuilder AddQuad(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            Vector3 normal,
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3,
            bool flipWinding = false)
        {
            normal.Normalize();

            var startIndex = (short)_vertices.Count;

            _vertices.Add(new VertexPositionNormalTexture(p0, normal, uv0));
            _vertices.Add(new VertexPositionNormalTexture(p1, normal, uv1));
            _vertices.Add(new VertexPositionNormalTexture(p2, normal, uv2));
            _vertices.Add(new VertexPositionNormalTexture(p3, normal, uv3));

            if (flipWinding)
            {
                _indices.Add(startIndex);
                _indices.Add((short)(startIndex + 2));
                _indices.Add((short)(startIndex + 1));
                _indices.Add(startIndex);
                _indices.Add((short)(startIndex + 3));
                _indices.Add((short)(startIndex + 2));
            }
            else
            {
                _indices.Add(startIndex);
                _indices.Add((short)(startIndex + 1));
                _indices.Add((short)(startIndex + 2));
                _indices.Add(startIndex);
                _indices.Add((short)(startIndex + 2));
                _indices.Add((short)(startIndex + 3));
            }

            return this;
        }

        public GridMeshPartData ToMeshPart()
        {
            return new GridMeshPartData(
                _textureKey,
                _vertices.ToArray(),
                _indices.ToArray());
        }
    }

    private static MeshPartBuilder GetOrCreate(this Dictionary<string, MeshPartBuilder> dict, string key)
    {
        if (!dict.TryGetValue(key, out var builder))
        {
            builder = new MeshPartBuilder(key);
            dict[key] = builder;
        }

        return builder;
    }
}
