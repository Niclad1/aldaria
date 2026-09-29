using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum AiActionKind
    {
        Move,
        Cast,
        EndTurn
    }

    public struct AiAction
    {
        public AiActionKind Kind;
        public Cell Target;
        public SpellDef Spell;
    }

    /// <summary>IA simples e gananciosa: ataca se pode, senão anda até poder atacar, senão se aproxima.</summary>
    public static class MonsterAI
    {
        public static AiAction Decide(Fight fight, Fighter me)
        {
            var enemies = fight.Fighters.FindAll(f => f.IsAlive && f.Team != me.Team);
            if (enemies.Count == 0) return End();

            // 1) Já dá para atacar daqui?
            var cast = BestCast(fight, me, me.Cell, enemies);
            if (cast.HasValue) return cast.Value;

            if (me.Mp <= 0) return End();

            // 2) Anda o mínimo possível até uma célula de onde algum feitiço alcance.
            var reach = fight.ReachableCells(me);
            Cell? bestCell = null;
            int bestCost = int.MaxValue;
            foreach (var kv in reach)
            {
                if (kv.Value == 0 || kv.Value >= bestCost) continue;
                if (BestCast(fight, me, kv.Key, enemies).HasValue)
                {
                    bestCell = kv.Key;
                    bestCost = kv.Value;
                }
            }
            if (bestCell.HasValue) return new AiAction { Kind = AiActionKind.Move, Target = bestCell.Value };

            // 3) Nada alcança: aproxima-se do inimigo mais perto.
            List<Cell> bestPath = null;
            foreach (var e in enemies)
            {
                var target = e.Cell;
                var path = Pathfinder.FindPath(me.Cell, target, c => c == target || fight.IsFree(c));
                if (path == null || path.Count < 2) continue;
                if (bestPath == null || path.Count < bestPath.Count) bestPath = path;
            }
            if (bestPath != null)
            {
                int steps = System.Math.Min(me.Mp, bestPath.Count - 1);
                if (steps > 0) return new AiAction { Kind = AiActionKind.Move, Target = bestPath[steps - 1] };
            }
            return End();
        }

        static AiAction? BestCast(Fight fight, Fighter me, Cell from, List<Fighter> enemies)
        {
            AiAction? best = null;
            int bestScore = int.MinValue;
            foreach (var s in me.Spells)
            {
                if (s.Effect != SpellEffect.Damage) continue;
                if (s.Target == SpellTarget.Self)
                {
                    // Explosão ao redor de si: vale a pena se pegar inimigos e não pegar aliados.
                    if (fight.CastProblem(me, s, from, from) != null) continue;
                    int hits = 0;
                    foreach (var other in fight.Fighters)
                    {
                        if (!other.IsAlive || other == me) continue;
                        var cell = other.Cell;
                        int d = cell.DistanceTo(from);
                        if (d == 0 || d > s.Area) continue;
                        hits += other.Team == me.Team ? -1 : 1;
                    }
                    if (hits <= 0) continue;
                    int areaScore = (s.Min + s.Max) * 10 * hits;
                    if (areaScore > bestScore)
                    {
                        bestScore = areaScore;
                        best = new AiAction { Kind = AiActionKind.Cast, Spell = s, Target = from };
                    }
                    continue;
                }
                foreach (var e in enemies)
                {
                    if (fight.CastProblem(me, s, e.Cell, from) != null) continue;
                    // Prefere o feitiço mais forte e o alvo mais machucado.
                    int score = (s.Min + s.Max) * 10 - e.Hp;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new AiAction { Kind = AiActionKind.Cast, Spell = s, Target = e.Cell };
                    }
                }
            }
            return best;
        }

        static AiAction End() => new AiAction { Kind = AiActionKind.EndTurn };
    }
}
