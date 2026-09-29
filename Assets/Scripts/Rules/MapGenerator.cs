using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    /// <summary>
    /// Gera o mapa de uma coordenada do mundo. É determinístico: a mesma coordenada
    /// sempre gera o mesmo mapa, então o servidor e todos os clientes enxergam igual.
    /// </summary>
    public static class MapGenerator
    {
        public const int Size = 17;

        static readonly Dictionary<Region, string[]> Names = new Dictionary<Region, string[]>
        {
            [Region.Meadow] = new[] { "Prado do Vento", "Campina Dourada", "Colinas Serenas", "Clareira dos Pipios", "Pasto Alto", "Lagoa Tranquila" },
            [Region.Forest] = new[] { "Estrada da Floresta", "Bosque Sussurrante", "Mata dos Javalis", "Trilha dos Lobos", "Floresta Densa" },
            [Region.Swamp] = new[] { "Pântano Sombrio", "Charco dos Sapos", "Brejo Enevoado", "Lamaçal Antigo" },
            [Region.Ruins] = new[] { "Ruínas Esquecidas", "Colunata Partida", "Campo de Ossos", "Portões Caídos" },
        };

        public static int Seed(int mapX, int mapY) => unchecked(mapX * 92821 + mapY * 68917 + 1337);

        public static GridMap Generate(int mapX, int mapY)
        {
            var rng = new Random(Seed(mapX, mapY));
            var region = Catalog.RegionOf(mapX, mapY);
            var map = new GridMap(Size, Size) { MapX = mapX, MapY = mapY, Region = region };

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
            int last = Size - 1;
            int mid = Size / 2;
            map.Name = IsBossMap(map) ? "Trono do Rei Lanudo" : Names[region][rng.Next(Names[region].Length)];

            double flowers = region == Region.Meadow ? 0.08 : region == Region.Forest ? 0.04 : 0.02;
            foreach (var c in map.Cells())
                map[c] = rng.NextDouble() < flowers ? Tile.Flowers : Tile.Grass;

            // Lagos: comuns no pântano, raros nas ruínas.
            int ponds = region == Region.Swamp ? rng.Next(2, 4) : region == Region.Ruins ? (rng.NextDouble() < 0.2 ? 1 : 0) : (rng.NextDouble() < 0.5 ? 1 : 0);
            for (int i = 0; i < ponds; i++)
            {
                var pond = new Cell(rng.Next(3, last - 3), rng.Next(3, last - 3));
                int r = rng.Next(1, region == Region.Swamp ? 4 : 3);
                foreach (var c in map.Cells())
                {
                    int d = c.DistanceTo(pond);
                    if (d < r || (d == r && rng.NextDouble() < 0.5)) map[c] = Tile.Water;
                }
            }

            double edge0, edge1, inner, rock, bush;
            switch (region)
            {
                case Region.Forest: edge0 = 0.55; edge1 = 0.25; inner = 0.08; rock = 0.02; bush = 0.04; break;
                case Region.Swamp: edge0 = 0.35; edge1 = 0.12; inner = 0.04; rock = 0.01; bush = 0.06; break;
                case Region.Ruins: edge0 = 0.25; edge1 = 0.06; inner = 0.015; rock = 0.05; bush = 0.02; break;
                default: edge0 = 0.42; edge1 = 0.12; inner = 0.03; rock = 0.03; bush = 0.025; break;
            }
            foreach (var c in map.Cells())
            {
                if (map[c] == Tile.Water) continue;
                int edge = Math.Min(Math.Min(c.X, c.Y), Math.Min(last - c.X, last - c.Y));
                double tree = edge == 0 ? edge0 : edge == 1 ? edge1 : inner;
                double roll = rng.NextDouble();
                if (roll < tree) map[c] = Tile.Tree;
                else if (roll < tree + rock) map[c] = Tile.Rock;
                else if (roll < tree + rock + bush) map[c] = Tile.Bush;
            }

            // Ruínas: colunas partidas espalhadas.
            if (region == Region.Ruins)
            {
                int pillars = rng.Next(4, 8);
                for (int i = 0; i < pillars; i++)
                {
                    var c = new Cell(rng.Next(2, last - 1), rng.Next(2, last - 1));
                    if (map[c] != Tile.Water) map[c] = Tile.Pillar;
                }
            }

            // Saídas no meio de cada borda, ligadas ao centro por trilhas.
            var center = map.Center;
            foreach (var exit in new[] { new Cell(mid, 0), new Cell(mid, last), new Cell(0, mid), new Cell(last, mid) })
            {
                map.Exits.Add(exit);
                CarvePath(map, exit, center, rng);
            }
            foreach (var c in map.Cells())
                if (c.DistanceTo(center) <= 1 && map[c] != Tile.Path) map[c] = Tile.Grass;
        }

        static bool IsBossMap(GridMap map) => map.MapX == Catalog.BossMapX && map.MapY == Catalog.BossMapY;

        /// <summary>A vila [0,0] tem um desenho fixo: praça, poço, casas e cercas.</summary>
        static void BuildVillage(GridMap map, Random rng)
        {
            int last = Size - 1;
            int mid = Size / 2;
            var center = map.Center;
            map.Name = "Vila de Aldaria";

            foreach (var c in map.Cells())
            {
                int edge = Math.Min(Math.Min(c.X, c.Y), Math.Min(last - c.X, last - c.Y));
                double roll = rng.NextDouble();
                if (edge == 0 && roll < 0.5) map[c] = Tile.Tree;
                else if (edge == 1 && roll < 0.1) map[c] = Tile.Tree;
                else map[c] = roll > 0.9 ? Tile.Flowers : Tile.Grass;
            }

            // Ruas principais e praça
            for (int i = 0; i < Size; i++)
            {
                map[new Cell(mid, i)] = Tile.Path;
                map[new Cell(i, mid)] = Tile.Path;
            }
            foreach (var c in map.Cells())
                if (c.DistanceTo(center) <= 2) map[c] = Tile.Path;
            map[center] = Tile.Well;

            // Casas 2x2
            foreach (var h in new[] { new Cell(3, 3), new Cell(11, 3), new Cell(3, 12), new Cell(12, 12) })
            {
                map.Houses.Add(h);
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                        map[new Cell(h.X + dx, h.Y + dy)] = Tile.House;
                // Canteiro de flores na frente de cada casa
                foreach (var f in new[] { new Cell(h.X + 2, h.Y), new Cell(h.X, h.Y + 2), new Cell(h.X + 2, h.Y + 1), new Cell(h.X + 1, h.Y + 2) })
                    if (map.InBounds(f) && map[f] == Tile.Grass) map[f] = Tile.Flowers;
            }

            // Cercas
            foreach (var f in new[] { new Cell(2, 6), new Cell(3, 6), new Cell(4, 6), new Cell(12, 10), new Cell(13, 10), new Cell(14, 10), new Cell(10, 2), new Cell(10, 3), new Cell(6, 13), new Cell(6, 14) })
                if (map[f] != Tile.Path) map[f] = Tile.Fence;

            foreach (var exit in new[] { new Cell(mid, 0), new Cell(mid, last), new Cell(0, mid), new Cell(last, mid) })
                map.Exits.Add(exit);
        }

        static void CarvePath(GridMap map, Cell from, Cell to, Random rng)
        {
            int last = map.Width - 1;
            var cur = from;
            var closer = new List<Cell>(2);
            var sideways = new List<Cell>(2);
            for (int guard = 0; guard < 120 && cur != to; guard++)
            {
                map[cur] = Tile.Path;
                closer.Clear();
                sideways.Clear();
                bool onEdge = cur.X == 0 || cur.Y == 0 || cur.X == last || cur.Y == last;
                foreach (var n in cur.Neighbors())
                {
                    if (!map.InBounds(n)) continue;
                    if (n.DistanceTo(to) < cur.DistanceTo(to)) closer.Add(n);
                    else if (!onEdge && n.X > 0 && n.Y > 0 && n.X < last && n.Y < last) sideways.Add(n);
                }
                // De vez em quando a trilha faz uma curva, para não ficar reta demais.
                bool wiggle = sideways.Count > 0 && cur.DistanceTo(to) > 2 && rng.NextDouble() < 0.25;
                var options = wiggle ? sideways : closer;
                cur = options[rng.Next(options.Count)];
            }
            // Garantia: termina em linha reta caso a curva tenha se alongado demais.
            while (cur != to)
            {
                map[cur] = Tile.Path;
                cur = cur.X != to.X ? new Cell(cur.X + Math.Sign(to.X - cur.X), cur.Y) : new Cell(cur.X, cur.Y + Math.Sign(to.Y - cur.Y));
            }
            map[to] = Tile.Path;
        }
    }
}
