using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    public static class Pathfinder
    {
        /// <summary>
        /// A* em 4 direções. Devolve o caminho sem a célula inicial e com a final,
        /// lista vazia se já está no destino, ou null se não há caminho.
        /// </summary>
        public static List<Cell> FindPath(Cell start, Cell goal, Func<Cell, bool> passable)
        {
            if (start == goal) return new List<Cell>();
            if (!passable(goal)) return null;

            var open = new List<Cell> { start };
            var cameFrom = new Dictionary<Cell, Cell>();
            var g = new Dictionary<Cell, int> { [start] = 0 };
            var closed = new HashSet<Cell>();

            while (open.Count > 0)
            {
                int bestIndex = 0;
                int bestF = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int f = g[open[i]] + open[i].DistanceTo(goal);
                    if (f < bestF)
                    {
                        bestF = f;
                        bestIndex = i;
                    }
                }

                var current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal) return Rebuild(cameFrom, start, goal);
                closed.Add(current);

                foreach (var next in current.Neighbors())
                {
                    if (closed.Contains(next) || !passable(next)) continue;
                    int cost = g[current] + 1;
                    if (g.TryGetValue(next, out int known) && known <= cost) continue;
                    g[next] = cost;
                    cameFrom[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
            return null;
        }

        /// <summary>Todas as células alcançáveis em até maxSteps passos, com a distância de cada uma (inclui a inicial com 0).</summary>
        public static Dictionary<Cell, int> Reachable(Cell start, int maxSteps, Func<Cell, bool> passable)
        {
            var dist = new Dictionary<Cell, int> { [start] = 0 };
            var queue = new Queue<Cell>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                int d = dist[c];
                if (d >= maxSteps) continue;
                foreach (var n in c.Neighbors())
                {
                    if (dist.ContainsKey(n) || !passable(n)) continue;
                    dist[n] = d + 1;
                    queue.Enqueue(n);
                }
            }
            return dist;
        }

        static List<Cell> Rebuild(Dictionary<Cell, Cell> cameFrom, Cell start, Cell goal)
        {
            var path = new List<Cell>();
            var c = goal;
            while (c != start)
            {
                path.Add(c);
                c = cameFrom[c];
            }
            path.Reverse();
            return path;
        }
    }
}
