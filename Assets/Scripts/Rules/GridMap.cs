using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum Tile : byte
    {
        Grass,
        Flowers,
        Path,
        Water,
        Tree,
        Rock,
        Bush,
        House,
        Fence,
        Well,
        Pillar
    }

    /// <summary>Um mapa do mundo: grid de células com o tipo de terreno de cada uma.</summary>
    public sealed class GridMap
    {
        public readonly int Width;
        public readonly int Height;
        public int MapX;
        public int MapY;
        public Region Region;
        public string Name = "";
        public readonly List<Cell> Exits = new List<Cell>();
        /// <summary>Canto de trás (menor x e y) de cada casa 2x2.</summary>
        public readonly List<Cell> Houses = new List<Cell>();

        readonly Tile[] tiles;

        public GridMap(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[width * height];
        }

        public Cell Center => new Cell(Width / 2, Height / 2);

        public bool InBounds(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

        public Tile this[Cell c]
        {
            get => tiles[c.Y * Width + c.X];
            set => tiles[c.Y * Width + c.X] = value;
        }

        public bool IsWalkable(Cell c)
        {
            if (!InBounds(c)) return false;
            var t = this[c];
            return t == Tile.Grass || t == Tile.Flowers || t == Tile.Path;
        }

        /// <summary>Árvores, pedras, casas, poços e colunas bloqueiam a linha de visão; água, cercas e arbustos não.</summary>
        public bool BlocksSight(Cell c)
        {
            if (!InBounds(c)) return true;
            var t = this[c];
            return t == Tile.Tree || t == Tile.Rock || t == Tile.House || t == Tile.Well || t == Tile.Pillar;
        }

        public IEnumerable<Cell> Cells()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    yield return new Cell(x, y);
        }
    }
}
