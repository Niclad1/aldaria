using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum ExitSide
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Gera o mapa de uma coordenada do mundo. É determinístico: a mesma coordenada
    /// sempre gera o mesmo mapa, então o servidor e todos os clientes enxergam igual.
    ///
    /// Formato: como no Dofus, cada mapa é um retângulo que ocupa a tela inteira
    /// (cerca de 15 x 8,5 unidades, ~540 células), desenhado sobre um grid isométrico.
    /// </summary>
    public static class MapGenerator
    {
        public const int Size = 33;
        /// <summary>Metade da largura em "diagonais": |x - y| &lt;= HalfWidth.</summary>
        public const int HalfWidth = 15;
        /// <summary>Metade da altura: |x + y - (Size - 1)| &lt;= HalfHeight.</summary>
        public const int HalfHeight = 17;

        static readonly Dictionary<Region, string[]> Names = new Dictionary<Region, string[]>
        {
            [Region.Meadow] = new[] { "Prado do Vento", "Campina Dourada", "Colinas Serenas", "Clareira dos Pipios", "Pasto Alto", "Lagoa Tranquila" },
            [Region.Forest] = new[] { "Estrada da Floresta", "Bosque Sussurrante", "Mata dos Javalis", "Trilha dos Lobos", "Floresta Densa" },
            [Region.Swamp] = new[] { "Pântano Sombrio", "Charco dos Sapos", "Brejo Enevoado", "Lamaçal Antigo" },
            [Region.Ruins] = new[] { "Ruínas Esquecidas", "Colunata Partida", "Campo de Ossos", "Portões Caídos" },
        };

        public static int Seed(int mapX, int mapY) => unchecked(mapX * 92821 + mapY * 68917 + 1337);

        /// <summary>Distância (em células) até a borda do retângulo; negativa = fora do mapa.</summary>
        public static int EdgeDistance(Cell c) =>
            Math.Min(HalfWidth - Math.Abs(c.X - c.Y), HalfHeight - Math.Abs(c.X + c.Y - (Size - 1)));

        public static Cell ExitCell(ExitSide side)
        {
            int m = Size / 2;
            switch (side)
            {
                case ExitSide.Up: return new Cell(m - HalfHeight / 2, m - HalfHeight / 2);
                case ExitSide.Down: return new Cell(m + HalfHeight / 2, m + HalfHeight / 2);
                case ExitSide.Left: return new Cell(m - HalfWidth / 2, m + HalfWidth / 2);
                default: return new Cell(m + HalfWidth / 2, m - HalfWidth / 2);
            }
        }

        public static ExitSide? SideOf(Cell exit)
        {
            foreach (ExitSide side in Enum.GetValues(typeof(ExitSide)))
                if (ExitCell(side) == exit) return side;
            return null;
        }

        public static ExitSide Opposite(ExitSide s) =>
            s == ExitSide.Up ? ExitSide.Down : s == ExitSide.Down ? ExitSide.Up : s == ExitSide.Left ? ExitSide.Right : ExitSide.Left;

        public static void Neighbor(int mapX, int mapY, ExitSide side, out int nx, out int ny)
        {
            nx = mapX + (side == ExitSide.Left ? -1 : side == ExitSide.Right ? 1 : 0);
            ny = mapY + (side == ExitSide.Up ? -1 : side == ExitSide.Down ? 1 : 0);
        }

        public static GridMap Generate(int mapX, int mapY)
        {
            var rng = new Random(Seed(mapX, mapY));
            var region = Catalog.RegionOf(mapX, mapY);
            var map = new GridMap(Size, Size) { MapX = mapX, MapY = mapY, Region = region };
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var c = new Cell(x, y);
                    map[c] = EdgeDistance(c) < 0 ? Tile.Void : Tile.Grass;
                }

            if (region == Region.Village) BuildVillage(map, rng);
            else BuildWild(map, rng);

            // NPCs precisam de chão livre em volta.
            foreach (var npc in Catalog.NpcsAt(mapX, mapY))
            {
                map[npc.Cell] = Tile.Grass;
                foreach (var n in npc.Cell.Neighbors())
                    if (map.InBounds(n) && !map.IsWalkable(n) && map[n] != Tile.House) map[n] = Tile.Grass;
            }

            // Garante que tudo que é caminhável esteja conectado ao centro.
            var reach = Pathfinder.Reachable(map.Center, int.MaxValue, map.IsWalkable);
            foreach (var c in map.Cells())
                if (map.IsWalkable(c) && !reach.ContainsKey(c)) map[c] = Tile.Bush;

            return map;
        }

        static void BuildWild(GridMap map, Random rng)
        {
            var region = map.Region;
            map.Name = IsBossMap(map) ? "Trono do Rei Lanudo" : Names[region][rng.Next(Names[region].Length)];

            double flowers = region == Region.Meadow ? 0.08 : region == Region.Forest ? 0.04 : 0.02;
            foreach (var c in map.Cells())
                map[c] = rng.NextDouble() < flowers ? Tile.Flowers : Tile.Grass;

            // Lagos: comuns no pântano, raros nas ruínas.
            int ponds = region == Region.Swamp ? rng.Next(3, 6) : region == Region.Ruins ? (rng.NextDouble() < 0.3 ? 1 : 0) : rng.Next(0, 3);
            var cells = new List<Cell>(map.Cells());
            for (int i = 0; i < ponds; i++)
            {
                Cell pond;
                do pond = cells[rng.Next(cells.Count)]; while (EdgeDistance(pond) < 4);
                int r = rng.Next(1, region == Region.Swamp ? 4 : 3);
                foreach (var c in cells)
                {
                    int d = c.DistanceTo(pond);
                    if (d < r || (d == r && rng.NextDouble() < 0.5)) map[c] = Tile.Water;
                }
            }

            double edge0, edge1, inner, rock, bush;
            switch (region)
            {
                case Region.Forest: edge0 = 0.6; edge1 = 0.3; inner = 0.07; rock = 0.02; bush = 0.04; break;
                case Region.Swamp: edge0 = 0.4; edge1 = 0.15; inner = 0.035; rock = 0.01; bush = 0.05; break;
                case Region.Ruins: edge0 = 0.3; edge1 = 0.1; inner = 0.015; rock = 0.04; bush = 0.02; break;
                default: edge0 = 0.5; edge1 = 0.2; inner = 0.025; rock = 0.025; bush = 0.025; break;
            }
            foreach (var c in cells)
            {
                if (map[c] == Tile.Water) continue;
                int edge = EdgeDistance(c);
                double tree = edge <= 0 ? edge0 : edge == 1 ? edge1 : inner;
                // Borda de baixo (mais perto da câmera): menos árvores altas para não tapar o mapa.
                if (c.X + c.Y > Size - 1 + HalfHeight - 3) tree *= 0.25;
                double roll = rng.NextDouble();
                if (roll < tree) map[c] = Tile.Tree;
                else if (roll < tree + rock) map[c] = Tile.Rock;
                else if (roll < tree + rock + bush) map[c] = Tile.Bush;
            }

            if (region == Region.Ruins)
            {
                int pillars = rng.Next(6, 11);
                for (int i = 0; i < pillars; i++)
                {
                    var c = cells[rng.Next(cells.Count)];
                    if (EdgeDistance(c) >= 2 && map[c] != Tile.Water) map[c] = Tile.Pillar;
                }
            }

            // Saídas no meio de cada lado, ligadas ao centro por trilhas.
            var center = map.Center;
            foreach (ExitSide side in Enum.GetValues(typeof(ExitSide)))
            {
                var exit = ExitCell(side);
                map.Exits.Add(exit);
                CarvePath(map, exit, center, rng, 0.25);
            }
            foreach (var c in cells)
                if (c.DistanceTo(center) <= 1 && map[c] != Tile.Path) map[c] = Tile.Grass;
        }

        static bool IsBossMap(GridMap map) => map.MapX == Catalog.BossMapX && map.MapY == Catalog.BossMapY;

        /// <summary>A vila [0,0] tem um desenho fixo: praça com poço, ruas em cruz, casas e cercas.</summary>
        static void BuildVillage(GridMap map, Random rng)
        {
            var center = map.Center;
            map.Name = "Vila de Aldaria";
            var cells = new List<Cell>(map.Cells());

            foreach (var c in cells)
            {
                int edge = EdgeDistance(c);
                double roll = rng.NextDouble();
                if (edge <= 0 && roll < 0.55) map[c] = Tile.Tree;
                else if (edge == 1 && roll < 0.15) map[c] = Tile.Tree;
                else map[c] = roll > 0.92 ? Tile.Flowers : Tile.Grass;
            }

            foreach (ExitSide side in Enum.GetValues(typeof(ExitSide)))
            {
                var exit = ExitCell(side);
                map.Exits.Add(exit);
                CarvePath(map, exit, center, rng, 0);
            }
            foreach (var c in cells)
                if (c.DistanceTo(center) <= 3) map[c] = Tile.Path;
            map[center] = Tile.Well;

            // Casas 2x2 (canto de trás), uma em cada quadrante entre as ruas e mais duas nas pontas.
            foreach (var h in new[] { new Cell(18, 9), new Cell(9, 18), new Cell(21, 15), new Cell(15, 21), new Cell(13, 7), new Cell(7, 13) })
            {
                bool free = true;
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                    {
                        var c = new Cell(h.X + dx, h.Y + dy);
                        if (!map.InBounds(c) || map[c] == Tile.Path || map[c] == Tile.Well) free = false;
                    }
                if (!free) continue;
                map.Houses.Add(h);
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                        map[new Cell(h.X + dx, h.Y + dy)] = Tile.House;
                foreach (var f in new[] { new Cell(h.X + 2, h.Y), new Cell(h.X, h.Y + 2), new Cell(h.X + 2, h.Y + 1), new Cell(h.X + 1, h.Y + 2) })
                    if (map.InBounds(f) && map[f] == Tile.Grass) map[f] = Tile.Flowers;
            }

            // Cercas curtas perto das casas
            foreach (var start in new[] { new Cell(21, 11), new Cell(11, 21), new Cell(24, 18), new Cell(18, 24) })
                for (int k = 0; k < 3; k++)
                {
                    var f = start.X > start.Y ? new Cell(start.X, start.Y + k) : new Cell(start.X + k, start.Y);
                    if (map.InBounds(f) && map[f] == Tile.Grass || map.InBounds(f) && map[f] == Tile.Flowers) map[f] = Tile.Fence;
                }
        }

        static void CarvePath(GridMap map, Cell from, Cell to, Random rng, double wiggleChance)
        {
            var cur = from;
            var closer = new List<Cell>(2);
            var sideways = new List<Cell>(2);
            for (int guard = 0; guard < 200 && cur != to; guard++)
            {
                map[cur] = Tile.Path;
                closer.Clear();
                sideways.Clear();
                foreach (var n in cur.Neighbors())
                {
                    if (!map.InBounds(n)) continue;
                    if (n.DistanceTo(to) < cur.DistanceTo(to)) closer.Add(n);
                    else if (EdgeDistance(n) >= 1 && EdgeDistance(cur) >= 1) sideways.Add(n);
                }
                // De vez em quando a trilha faz uma curva, para não ficar reta demais.
                bool wiggle = sideways.Count > 0 && cur.DistanceTo(to) > 2 && rng.NextDouble() < wiggleChance;
                var options = wiggle ? sideways : closer;
                if (options.Count == 0) break;
                // Sem curvas: alterna os passos para a trilha seguir reta na tela (em escada).
                cur = wiggleChance == 0 && options.Count > 1 ? options[guard % 2] : options[rng.Next(options.Count)];
            }
            while (cur != to)
            {
                map[cur] = Tile.Path;
                cur = cur.X != to.X ? new Cell(cur.X + Math.Sign(to.X - cur.X), cur.Y) : new Cell(cur.X, cur.Y + Math.Sign(to.Y - cur.Y));
            }
            map[to] = Tile.Path;
        }
    }
}
