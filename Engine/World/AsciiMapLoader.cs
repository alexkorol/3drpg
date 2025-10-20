using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rpg3D.Engine.World;

public static class AsciiMapLoader
{
    public static GridMap FromFile(string path)
    {
        var lines = File.ReadAllLines(path);
        return FromLines(lines);
    }

    public static GridMap FromLines(IEnumerable<string> lines)
    {
        var lineList = lines
            .Where(line => line is not null)
            .Select(line => line.TrimEnd('\r'))
            .ToList();

        if (lineList.Count == 0)
        {
            throw new InvalidOperationException("ASCII map is empty.");
        }

        var width = lineList.Max(l => l.Length);
        var height = lineList.Count;
        var cells = new CellType[width * height];

        for (var y = 0; y < height; y++)
        {
            var line = lineList[y];
            for (var x = 0; x < width; x++)
            {
                var glyph = x < line.Length ? line[x] : ' ';
                cells[(y * width) + x] = ParseGlyph(glyph);
            }
        }

        return new GridMap(width, height, cells);
    }

    private static CellType ParseGlyph(char glyph) => glyph switch
    {
        '#' => CellType.Wall,
        '^' => CellType.StairUp,
        'v' => CellType.StairDown,
        '~' => CellType.Water,
        _ => CellType.Empty
    };
}
