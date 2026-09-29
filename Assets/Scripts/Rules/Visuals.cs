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

        // Listas com repetição = peso. Várias espécies por bioma deixam a mata menos "carimbada".
        static readonly System.Collections.Generic.Dictionary<Region, string[]> Trees = new System.Collections.Generic.Dictionary<Region, string[]>
        {
            [Region.Village] = new[] { "tree_village_0", "tree_village_1", "tree_apple", "tree_young", "tree_birch", "tree_meadow_0", "tree_meadow_1", "tree_oak_big", "tree_cypress" },
            [Region.Meadow] = new[] { "tree_meadow_0", "tree_meadow_0", "tree_meadow_1", "tree_meadow_1", "tree_meadow_2", "tree_birch", "tree_birch", "tree_oak_big", "tree_cypress", "tree_young", "tree_apple" },
            [Region.Forest] = new[] { "tree_forest_0", "tree_forest_1", "tree_forest_2", "tree_forest_2", "tree_spruce", "tree_spruce", "tree_oak_forest", "tree_birch" },
            [Region.Swamp] = new[] { "tree_swamp_0", "tree_swamp_0", "tree_swamp_1", "tree_cypress_swamp", "tree_cypress_swamp", "tree_forest_2" },
            [Region.Ruins] = new[] { "tree_ruins_0", "tree_ruins_1", "tree_ruins_2", "tree_ruins_2", "tree_cypress" },
        };

        static readonly System.Collections.Generic.Dictionary<Region, string[]> Rocks = new System.Collections.Generic.Dictionary<Region, string[]>
        {
            [Region.Village] = new[] { "crates", "barrels", "haystack", "crates", "barrels", "stall_0", "stall_1" },
            [Region.Meadow] = new[] { "rock_0", "rock_1", "stump", "log", "haystack" },
            [Region.Forest] = new[] { "rock_moss_0", "rock_moss_1", "stump_moss", "log", "stump" },
            [Region.Swamp] = new[] { "rock_moss_0", "rock_moss_1", "log", "stump_moss" },
            [Region.Ruins] = new[] { "rock_0", "rock_1", "rubble", "rubble" },
        };

        static readonly System.Collections.Generic.Dictionary<Region, string[]> Bushes = new System.Collections.Generic.Dictionary<Region, string[]>
        {
            [Region.Village] = new[] { "bush_village_0", "bush_village_1", "bench", "bush_village_0" },
            [Region.Meadow] = new[] { "bush_meadow_0", "bush_meadow_1" },
            [Region.Forest] = new[] { "bush_forest_0", "bush_forest_1", "mushrooms" },
            [Region.Swamp] = new[] { "bush_swamp_0", "bush_swamp_1", "mushrooms_swamp" },
            [Region.Ruins] = new[] { "bush_ruins_0", "bush_ruins_1" },
        };

        static string Pick(System.Collections.Generic.Dictionary<Region, string[]> table, Region r, int v)
        {
            var list = table[r];
            return list[v % list.Length];
        }

        public static string Tree(Region r, int v) => Pick(Trees, r, v);

        /// <summary>Desenho em pé na célula (árvore, pedra...), ou null. Casas são tratadas à parte (ocupam 2x2).</summary>
        public static string Prop(GridMap m, Cell c)
        {
            int v = Variant(m, c) / 7;
            switch (m[c])
            {
                case Tile.Tree: return Pick(Trees, m.Region, v);
                case Tile.Rock: return Pick(Rocks, m.Region, v);
                case Tile.Bush: return Pick(Bushes, m.Region, v);
                case Tile.Fence:
                    bool alongX = IsFence(m, new Cell(c.X - 1, c.Y)) || IsFence(m, new Cell(c.X + 1, c.Y));
                    return alongX ? "fence_x" : "fence_y";
                case Tile.Well:
                    return "well";
                case Tile.Pillar:
                    if (m.Region == Region.Village) return v % 5 == 0 ? "signpost" : "lamp";
                    return $"pillar_{v % 2}";
                default:
                    return null;
            }
        }

        /// <summary>Espelha o desenho? (variação por instância; cercas e poço têm direção, ficam como estão.)</summary>
        public static bool PropFlip(GridMap m, Cell c)
        {
            var t = m[c];
            if (t == Tile.Fence || t == Tile.Well || t == Tile.House) return false;
            return (Variant(m, c) / 3) % 2 == 1;
        }

        /// <summary>Escala da instância: árvores variam mais (0,85–1,15), o resto pouco.</summary>
        public static float PropScale(GridMap m, Cell c)
        {
            var t = m[c];
            if (t == Tile.Fence || t == Tile.Well || t == Tile.House || t == Tile.Pillar) return 1f;
            float k = (Variant(m, c) / 11 % 31) / 30f;
            return t == Tile.Tree ? 0.85f + 0.3f * k : 0.92f + 0.14f * k;
        }

        static readonly System.Collections.Generic.Dictionary<Region, string[]> DecorKinds = new System.Collections.Generic.Dictionary<Region, string[]>
        {
            [Region.Village] = new[] { "tufts", "flowers", "pebbles", "clover" },
            [Region.Meadow] = new[] { "tufts", "flowers", "pebbles", "clover" },
            [Region.Forest] = new[] { "tufts", "mush", "leaves", "pebbles" },
            [Region.Swamp] = new[] { "tufts", "mush", "clover", "pebbles" },
            [Region.Ruins] = new[] { "tufts", "pebbles", "bones", "leaves" },
        };

        /// <summary>Detalhe pequeno no chão (tufos, pedrinhas, cogumelos...): não bloqueia e fica sob os personagens.</summary>
        public static string Decor(GridMap m, Cell c)
        {
            var t = m[c];
            if (t != Tile.Grass && t != Tile.Flowers) return null;
            int v = Variant(m, c);
            if (v / 13 % 100 >= 24) return null;
            var kinds = DecorKinds[m.Region];
            return $"decor_{Biome(m.Region)}_{kinds[v / 17 % kinds.Length]}_{v / 5 % 2}";
        }

        static readonly System.Collections.Generic.Dictionary<Region, string[]> PatchKinds = new System.Collections.Generic.Dictionary<Region, string[]>
        {
            [Region.Village] = new[] { "shade", "sun", "flowers", "stones" },
            [Region.Meadow] = new[] { "shade", "sun", "flowers", "stones" },
            [Region.Forest] = new[] { "shade", "leaves", "sun", "stones" },
            [Region.Swamp] = new[] { "shade", "moss", "flowers", "stones" },
            [Region.Ruins] = new[] { "shade", "sand", "stones", "sun" },
        };

        /// <summary>Mancha grande de chão (grama escura, flores, pedrinhas...) centrada nesta célula, ou null.</summary>
        public static string Patch(GridMap m, Cell c)
        {
            var t = m[c];
            if (t == Tile.Void || t == Tile.Path || t == Tile.Water || t == Tile.Well) return null;
            int v = Variant(m, c);
            if (v % 7 != 0) return null;
            var kinds = PatchKinds[m.Region];
            return $"patch_{Biome(m.Region)}_{kinds[(v / 7) % kinds.Length]}_{(v / 29) % 2}";
        }

        /// <summary>Largura da moldura de cenário desenhada fora do retângulo jogável (esconde a borda do mapa).</summary>
        public const int BorderDepth = 6;

        /// <summary>Chão da moldura (fora do mapa jogável).</summary>
        public static string BorderGround(GridMap m, Cell c) => $"tile_{Biome(m.Region)}_grass_{Variant(m, c) % 3}";

        /// <summary>Árvores, arbustos e pedras densos na moldura, para o mapa não parecer uma ilha.</summary>
        public static string BorderProp(GridMap m, Cell c)
        {
            int v = Variant(m, c);
            int r = v % 100;
            string b = Biome(m.Region);
            // Na faixa de baixo (perto de quem olha), só vegetação baixa, para não tapar o mapa.
            bool front = c.X + c.Y > MapGenerator.Size - 1 + MapGenerator.HalfHeight - 2 && MapGenerator.EdgeDistance(c) > -4;
            if (front) r = 62 + r % 38;
            if (r < 62) return Pick(Trees, m.Region, v / 7);
            if (r < 82) return $"bush_{b}_{v / 7 % 2}";
            if (r < 88) return m.Region == Region.Village ? $"bush_village_{v / 7 % 2}" : Pick(Rocks, m.Region, v / 7);
            return null;
        }

        public const int HouseVariants = 6;

        /// <summary>Casa desta âncora: casas do mesmo mapa recebem modelos diferentes.</summary>
        public static string House(GridMap m, Cell anchor)
        {
            int i = m.Houses.IndexOf(anchor);
            if (i < 0) i = Variant(m, anchor);
            return $"house_{(i + (m.MapX * 7 + m.MapY * 3 + 100)) % HouseVariants}";
        }

        /// <summary>Espelha a moldura também, para a borda não repetir o mesmo desenho lado a lado.</summary>
        public static bool BorderFlip(GridMap m, Cell c) => (Variant(m, c) / 3) % 2 == 1;

        public static float BorderScale(GridMap m, Cell c) => 0.85f + 0.3f * ((Variant(m, c) / 11 % 31) / 30f);

        static bool IsFence(GridMap m, Cell c) => m[c] == Tile.Fence;
    }
}
