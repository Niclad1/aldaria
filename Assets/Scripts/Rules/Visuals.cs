namespace Aldaria.Rules
{
    /// <summary>
    /// Escolhe o nome do desenho de cada célula (chão e cenário). Fica nas regras para que
    /// o jogo e as ferramentas de prévia usem exatamente a mesma escolha.
    /// </summary>
    public static class Visuals
    {
        public static string Biome(Region r)
        {
            switch (r)
            {
                case Region.Village: return "village";
                case Region.Forest: return "forest";
                case Region.Swamp: return "swamp";
                case Region.Ruins: return "ruins";
                default: return "meadow";
            }
        }

        public static int Variant(GridMap m, Cell c)
        {
            unchecked
            {
                uint h = (uint)(c.X * 374761393 + c.Y * 668265263 + m.MapX * 144665 + m.MapY * 9737333);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (int)((h ^ (h >> 16)) & 0x7FFFFFFF);
            }
        }

        public static string Ground(GridMap m, Cell c)
        {
            string b = Biome(m.Region);
            int v = Variant(m, c);
            switch (m[c])
            {
                case Tile.Path: return $"tile_{b}_grass_{v % 3}";
                case Tile.Water: return $"tile_{b}_water_0";
                case Tile.Flowers: return $"tile_{b}_flowers_0";
                case Tile.Well: return $"tile_{b}_path_0";
                default: return $"tile_{b}_grass_{v % 3}";
            }
        }

        /// <summary>Caminho desenhado por cima do chão (maior que a célula, para as trilhas ficarem contínuas), ou null.</summary>
        public static string Decal(GridMap m, Cell c)
        {
            if (m[c] != Tile.Path && m[c] != Tile.Well) return null;
            return $"decal_{Biome(m.Region)}_path_{Variant(m, c) % 2}";
        }

        /// <summary>Desenho em pé na célula (árvore, pedra...), ou null. Casas são tratadas à parte (ocupam 2x2).</summary>
        public static string Prop(GridMap m, Cell c)
        {
            string b = Biome(m.Region);
            int v = Variant(m, c) / 7;
            switch (m[c])
            {
                case Tile.Tree:
                    switch (m.Region)
                    {
                        case Region.Meadow: return $"tree_meadow_{(v % 9 == 0 ? 2 : v % 2)}";
                        case Region.Forest: return $"tree_forest_{v % 3}";
                        default: return $"tree_{b}_{v % 2}";
                    }
                case Tile.Rock:
                    return m.Region == Region.Forest || m.Region == Region.Swamp ? $"rock_moss_{v % 2}" : $"rock_{v % 2}";
                case Tile.Bush:
                    return $"bush_{b}_{v % 2}";
                case Tile.Fence:
                    bool alongX = IsFence(m, new Cell(c.X - 1, c.Y)) || IsFence(m, new Cell(c.X + 1, c.Y));
                    return alongX ? "fence_x" : "fence_y";
                case Tile.Well:
                    return "well";
                case Tile.Pillar:
                    return $"pillar_{v % 2}";
                default:
                    return null;
            }
        }

        public static string House(GridMap m, Cell anchor) => $"house_{Variant(m, anchor) % 2}";

        static bool IsFence(GridMap m, Cell c) => m[c] == Tile.Fence;
    }
}
