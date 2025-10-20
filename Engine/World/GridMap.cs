using System;

namespace Rpg3D.Engine.World;

public sealed class GridMap
{
    private readonly CellType[] _cells;

    public GridMap(int width, int height, CellType[] cells)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (cells.Length != width * height)
        {
            throw new ArgumentException("Cell buffer size does not match map dimensions.", nameof(cells));
        }

        Width = width;
        Height = height;
        _cells = cells;
    }

    public int Width { get; }

    public int Height { get; }

    public CellType this[int x, int y]
    {
        get
        {
            if ((uint)x >= Width || (uint)y >= Height)
            {
                return CellType.Empty;
            }

            return _cells[(y * Width) + x];
        }
    }

    public bool IsBlocked(int x, int y)
    {
        return this[x, y] == CellType.Wall;
    }
}
