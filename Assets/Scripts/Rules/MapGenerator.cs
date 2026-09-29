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

        static readonly string[] Names =
        {
            "Prado do Vento", "Bosque dos Lanudos", "Colinas Serenas", "Clareira dos Pipios",
            "Trilha do Cogumelo", "Vale Esquecido", "Campina Dourada", "Orla da Floresta",
            "Pasto Alto", "Lagoa Tranquila", "Sendas Antigas", "Bosque Sussurrante"
        };

        public static int Seed(int mapX, int mapY) => unchecked(mapX * 92821 + mapY * 68917 + 1337);

        public static GridMap Generate(int mapX, int mapY)
        {
            var rng = new Random(Seed(mapX, mapY));
            var map = new GridMap(Size, Size) { MapX = mapX, MapY = mapY };
            map.Name = mapX == 0 && mapY == 0 ? "Campos de Aldaria" : Names[rng.Next(Names.Length)];
            int last = Size - 1;
            int mid = Size / 2;

            // Grama com manchas de flores.
            foreach (var c in map.Cells())
                map[c] = rng.NextDouble() < 0.07 ? Tile.Flowers : Tile.Grass;

            // Lago em metade dos mapas.
            if (rng.NextDouble() < 0.5)
            {
                var pond = new Cell(rng.Next(3, last - 3), rng.Next(3, last - 3));
                int r = rng.Next(1, 3);
                foreach (var c in map.Cells())
                {
                    int d = c.DistanceTo(pond);
                    if (d < r || (d == r && rng.NextDouble() < 0.5)) map[c] = Tile.Water;
                }
            }

            // Árvores mais densas nas bordas; pedras e arbustos espalhados.
            foreach (var c in map.Cells())
            {
                if (map[c] == Tile.Water) continue;
                int edge = Math.Min(Math.Min(c.X, c.Y), Math.Min(last - c.X, last - c.Y));
                double tree = edge == 0 ? 0.42 : edge == 1 ? 0.12 : 0.03;
                double roll = rng.NextDouble();
                if (roll < tree) map[c] = Tile.Tree;
                else if (roll < tree + 0.03) map[c] = Tile.Rock;
                else if (roll < tree + 0.055) map[c] = Tile.Bush;
            }

            // Saídas no meio de cada borda, ligadas ao centro por trilhas.
            var center = map.Center;
            var exits = new[] { new Cell(mid, 0), new Cell(mid, last), new Cell(0, mid), new Cell(last, mid) };
            foreach (var exit in exits)
            {
                map.Exits.Add(exit);
                CarvePath(map, exit, center, rng);
            }
            foreach (var c in map.Cells())
                if (c.DistanceTo(center) <= 1 && map[c] != Tile.Path) map[c] = Tile.Grass;

            // Garante que tudo que é caminhável esteja conectado ao centro.
            var reach = Pathfinder.Reachable(center, int.MaxValue, map.IsWalkable);
            foreach (var c in map.Cells())
                if (map.IsWalkable(c) && !reach.ContainsKey(c)) map[c] = Tile.Bush;

            return map;
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
