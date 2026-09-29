using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum FightPhase
    {
        Placement,
        Fighting,
        Ended
    }

    public enum FightEventKind
    {
        FightStarted,
        TurnStarted,
        TurnEnded,
        Moved,
        SpellCast,
        Damaged,
        Healed,
        Pushed,
        Teleported,
        BuffApplied,
        Died,
        FightEnded
    }

    /// <summary>
    /// Algo que aconteceu na luta. A camada visual consome esses eventos em ordem para animar;
    /// num servidor, são exatamente as mensagens que iriam para os clientes.
    /// </summary>
    public sealed class FightEvent
    {
        public FightEventKind Kind;
        public Fighter Source;
        public Fighter Target;
        public SpellDef Spell;
        public int Value;
        public Cell Cell;
        public List<Cell> Path;
        public string Text;
    }

    /// <summary>Regras de uma luta por turnos. Não depende da Unity.</summary>
    public sealed class Fight
    {
        public readonly GridMap Map;
        public readonly List<Fighter> Fighters = new List<Fighter>();
        public readonly Queue<FightEvent> Events = new Queue<FightEvent>();
        public readonly Dictionary<Team, List<Cell>> PlacementCells = new Dictionary<Team, List<Cell>>
        {
            [Team.Players] = new List<Cell>(),
            [Team.Monsters] = new List<Cell>(),
        };

        public FightPhase Phase { get; private set; } = FightPhase.Placement;
        public int Round { get; private set; }
        public Team Winner { get; private set; }
        public IReadOnlyList<Fighter> TurnOrder => order;
        public Fighter Current => Phase == FightPhase.Fighting ? order[turnIndex] : null;

        readonly List<Fighter> order = new List<Fighter>();
        readonly Random rng;
        int turnIndex;

        public Fight(GridMap map, int seed)
        {
            Map = map;
            rng = new Random(seed);
        }

        // ---------------------------------------------------------------- preparação

        public void AddFighter(Fighter f, Cell cell)
        {
            f.Cell = cell;
            Fighters.Add(f);
        }

        /// <summary>Na fase de posicionamento, troca de célula (ou de lugar com um aliado).</summary>
        public bool TryPlace(Fighter f, Cell cell)
        {
            if (Phase != FightPhase.Placement || !PlacementCells[f.Team].Contains(cell)) return false;
            var other = FighterAt(cell);
            if (other == f) return false;
            if (other != null)
            {
                if (other.Team != f.Team) return false;
                other.Cell = f.Cell;
            }
            f.Cell = cell;
            return true;
        }

        public void Start()
        {
            if (Phase != FightPhase.Placement) return;
            order.Clear();
            order.AddRange(Fighters);
            // Ordem por iniciativa (estável: em empate, quem entrou primeiro joga antes).
            var index = new Dictionary<Fighter, int>();
            for (int i = 0; i < order.Count; i++) index[order[i]] = i;
            order.Sort((a, b) => a.Initiative != b.Initiative ? b.Initiative.CompareTo(a.Initiative) : index[a].CompareTo(index[b]));

            Phase = FightPhase.Fighting;
            Round = 1;
            turnIndex = 0;
            Emit(new FightEvent { Kind = FightEventKind.FightStarted });
            if (!BeginTurn(order[0])) AdvanceTurn();
        }

        // ---------------------------------------------------------------- consultas

        public Fighter FighterAt(Cell c)
        {
            foreach (var f in Fighters)
                if (f.IsAlive && f.Cell == c) return f;
            return null;
        }

        public bool IsFree(Cell c) => Map.IsWalkable(c) && FighterAt(c) == null;

        public Dictionary<Cell, int> ReachableCells(Fighter f) => Pathfinder.Reachable(f.Cell, f.Mp, IsFree);

        /// <summary>Caminho até a célula se couber nos PM atuais; senão null.</summary>
        public List<Cell> PathFor(Fighter f, Cell target)
        {
            if (!IsFree(target)) return null;
            var path = Pathfinder.FindPath(f.Cell, target, IsFree);
            if (path == null || path.Count == 0 || path.Count > f.Mp) return null;
            return path;
        }

        /// <summary>Por que o feitiço não pode ser lançado ali (null = pode). "origin" permite simular outra posição (usado pela IA).</summary>
        public string CastProblem(Fighter f, SpellDef s, Cell target, Cell? origin = null)
        {
            var from = origin ?? f.Cell;
            if (f.Ap < s.ApCost) return "PA insuficientes";
            int cd = f.CooldownOf(s);
            if (cd > 0) return $"Recarregando ({cd} turno{(cd > 1 ? "s" : "")})";
            if (f.CastsOf(s) >= s.MaxCastsPerTurn) return "Limite de usos neste turno";
            if (!Map.InBounds(target)) return "Fora do mapa";

            int d = from.DistanceTo(target);
            if (d < s.MinRange || d > s.MaxRange) return "Fora de alcance";
            if (s.InLineOnly && !from.IsInLineWith(target)) return "Só em linha reta";

            var occupant = OccupantAssuming(target, f, from);
            switch (s.Target)
            {
                case SpellTarget.Fighter:
                    if (occupant == null) return "Escolha um alvo";
                    break;
                case SpellTarget.EmptyCell:
                    if (occupant != null || !Map.IsWalkable(target)) return "A célula precisa estar livre";
                    break;
                case SpellTarget.Self:
                    if (target != from) return "Só em você mesmo";
                    break;
                case SpellTarget.AnyCell:
                    if (Map.BlocksSight(target)) return "Célula inválida";
                    break;
            }

            if (s.NeedsSight && !HasSight(f, from, target)) return "Sem linha de visão";
            return null;
        }

        public bool HasSight(Fighter f, Cell from, Cell target) =>
            LineOfSight.Has(from, target, c => Map.BlocksSight(c) || OccupantAssuming(c, f, from) != null);

        /// <summary>Células dentro do alcance do feitiço (sem checar visão) — para desenhar o alcance.</summary>
        public List<Cell> RangeCells(Fighter f, SpellDef s)
        {
            var list = new List<Cell>();
            for (int y = f.Cell.Y - s.MaxRange; y <= f.Cell.Y + s.MaxRange; y++)
            {
                for (int x = f.Cell.X - s.MaxRange; x <= f.Cell.X + s.MaxRange; x++)
                {
                    var c = new Cell(x, y);
                    if (!Map.InBounds(c)) continue;
                    int d = f.Cell.DistanceTo(c);
                    if (d < s.MinRange || d > s.MaxRange) continue;
                    if (s.InLineOnly && !f.Cell.IsInLineWith(c)) continue;
                    if (Map[c] == Tile.Tree || Map[c] == Tile.Rock) continue;
                    list.Add(c);
                }
            }
            return list;
        }

        public List<Cell> AreaCells(SpellDef s, Cell target)
        {
            var list = new List<Cell>();
            for (int y = target.Y - s.Area; y <= target.Y + s.Area; y++)
                for (int x = target.X - s.Area; x <= target.X + s.Area; x++)
                {
                    var c = new Cell(x, y);
                    if (Map.InBounds(c) && c.DistanceTo(target) <= s.Area) list.Add(c);
                }
            return list;
        }

        // ---------------------------------------------------------------- ações

        public bool TryMove(Fighter f, Cell target)
        {
            if (!CanAct(f)) return false;
            var path = PathFor(f, target);
            if (path == null) return false;
            f.Mp -= path.Count;
            f.Cell = target;
            Emit(new FightEvent { Kind = FightEventKind.Moved, Source = f, Path = path, Cell = target });
            return true;
        }

        public bool TryCast(Fighter f, SpellDef s, Cell target)
        {
            if (!CanAct(f) || CastProblem(f, s, target) != null) return false;

            f.Ap -= s.ApCost;
            f.CastsThisTurn[s.Id] = f.CastsOf(s) + 1;
            if (s.Cooldown > 0) f.Cooldowns[s.Id] = s.Cooldown;
            Emit(new FightEvent { Kind = FightEventKind.SpellCast, Source = f, Spell = s, Cell = target });

            switch (s.Effect)
            {
                case SpellEffect.Damage:
                    foreach (var victim in VictimsOf(f, s, target))
                    {
                        int dealt = Damage(f, victim, Roll(f, s, true));
                        if (s.LifeSteal > 0 && f.IsAlive && dealt > 0) Heal(f, f, Math.Max(1, dealt * s.LifeSteal / 100));
                        if (!victim.IsAlive) continue;
                        var pushOrigin = s.Area > 0 && victim.Cell != target ? target : f.Cell;
                        if (s.Push > 0) Push(f, victim, pushOrigin, s.Push);
                        if (s.Pull > 0) Pull(f, victim, s.Pull);
                        if (s.MpSteal > 0 && victim.IsAlive)
                        {
                            victim.Buffs.Add(new Buff { Stat = BuffStat.Mp, Value = -s.MpSteal, TurnsLeft = 1, Source = s.Name });
                            Emit(new FightEvent { Kind = FightEventKind.BuffApplied, Source = f, Target = victim, Value = -s.MpSteal, Text = $"-{s.MpSteal} PM" });
                        }
                        if (s.PoisonDamage > 0 && victim.IsAlive)
                        {
                            victim.Buffs.Add(new Buff { Stat = BuffStat.Poison, Value = s.PoisonDamage, TurnsLeft = s.PoisonTurns, Source = s.Name });
                            Emit(new FightEvent { Kind = FightEventKind.BuffApplied, Source = f, Target = victim, Value = s.PoisonDamage, Text = "Envenenado!" });
                        }
                    }
                    break;

                case SpellEffect.Heal:
                    foreach (var ally in VictimsOf(f, s, target)) Heal(f, ally, Roll(f, s, false));
                    break;

                case SpellEffect.Teleport:
                    var from = f.Cell;
                    f.Cell = target;
                    Emit(new FightEvent { Kind = FightEventKind.Teleported, Source = f, Target = f, Cell = target, Path = new List<Cell> { from, target } });
                    break;

                case SpellEffect.Buff:
                    f.Buffs.Add(new Buff { Stat = s.BuffStat, Value = s.Min, TurnsLeft = s.BuffTurns, Source = s.Name });
                    // Bônus de PA/PM vale já neste turno.
                    if (s.BuffStat == BuffStat.Ap) f.Ap += s.Min;
                    if (s.BuffStat == BuffStat.Mp) f.Mp += s.Min;
                    Emit(new FightEvent { Kind = FightEventKind.BuffApplied, Source = f, Target = f, Value = s.Min, Text = $"+{s.Min} {BuffLabel(s.BuffStat)}" });
                    break;
            }

            CheckEnd();
            // Se o jogador da vez morreu na própria jogada (dano de área, por exemplo), passa a vez.
            if (Phase == FightPhase.Fighting && !f.IsAlive) EndTurn();
            return true;
        }

        public void EndTurn()
        {
            if (Phase != FightPhase.Fighting) return;
            var f = Current;

            var keys = new List<string>(f.Cooldowns.Keys);
            foreach (var k in keys)
                if (f.Cooldowns[k] > 0) f.Cooldowns[k]--;
            for (int i = f.Buffs.Count - 1; i >= 0; i--)
                if (--f.Buffs[i].TurnsLeft <= 0) f.Buffs.RemoveAt(i);

            Emit(new FightEvent { Kind = FightEventKind.TurnEnded, Source = f });
            AdvanceTurn();
        }

        // ---------------------------------------------------------------- internos

        bool CanAct(Fighter f) => Phase == FightPhase.Fighting && f == Current && f.IsAlive;

        /// <summary>Passa para o próximo lutador vivo. O veneno é aplicado no início do turno e pode derrubar alguém antes de jogar.</summary>
        void AdvanceTurn()
        {
            for (int guard = 0; guard < order.Count * 2 && Phase == FightPhase.Fighting; guard++)
            {
                turnIndex++;
                if (turnIndex >= order.Count)
                {
                    turnIndex = 0;
                    Round++;
                }
                var next = order[turnIndex];
                if (!next.IsAlive) continue;
                if (BeginTurn(next)) return;
            }
        }

        /// <summary>Começa o turno de alguém. Devolve false se o veneno o derrubou.</summary>
        bool BeginTurn(Fighter f)
        {
            foreach (var b in f.Buffs.ToArray())
            {
                if (b.Stat != BuffStat.Poison || !f.IsAlive) continue;
                Damage(null, f, b.Value, b.Source);
            }
            if (!f.IsAlive)
            {
                CheckEnd();
                return false;
            }
            f.Ap = Math.Max(0, f.BaseAp + f.BuffTotal(BuffStat.Ap));
            f.Mp = Math.Max(0, f.BaseMp + f.BuffTotal(BuffStat.Mp));
            f.CastsThisTurn.Clear();
            Emit(new FightEvent { Kind = FightEventKind.TurnStarted, Source = f });
            return true;
        }

        Fighter OccupantAssuming(Cell c, Fighter self, Cell selfCell)
        {
            if (c == selfCell) return self;
            var o = FighterAt(c);
            return o == self ? null : o;
        }

        List<Fighter> VictimsOf(Fighter caster, SpellDef s, Cell target)
        {
            var list = new List<Fighter>();
            foreach (var c in AreaCells(s, target))
            {
                var f = FighterAt(c);
                if (f != null && !(s.ExcludeCaster && f == caster)) list.Add(f);
            }
            return list;
        }

        int Roll(Fighter f, SpellDef s, bool isDamage)
        {
            int v = rng.Next(s.Min, s.Max + 1);
            if (isDamage) v += f.BuffTotal(BuffStat.Damage) + f.FlatDamage;
            return Math.Max(0, v * (100 + f.Power) / 100);
        }

        int Damage(Fighter source, Fighter target, int amount, string cause = null)
        {
            amount = Math.Min(amount, target.Hp);
            target.Hp -= amount;
            Emit(new FightEvent { Kind = FightEventKind.Damaged, Source = source, Target = target, Value = amount, Text = cause });
            if (!target.IsAlive) Emit(new FightEvent { Kind = FightEventKind.Died, Target = target });
            return amount;
        }

        void Heal(Fighter source, Fighter target, int amount)
        {
            amount = Math.Min(amount, target.MaxHp - target.Hp);
            target.Hp += amount;
            Emit(new FightEvent { Kind = FightEventKind.Healed, Source = source, Target = target, Value = amount });
        }

        void Pull(Fighter source, Fighter target, int cells)
        {
            int dx = source.Cell.X - target.Cell.X;
            int dy = source.Cell.Y - target.Cell.Y;
            if (dx == 0 && dy == 0) return;
            var step = Math.Abs(dx) >= Math.Abs(dy) ? new Cell(Math.Sign(dx), 0) : new Cell(0, Math.Sign(dy));
            var start = target.Cell;
            for (int i = 0; i < cells; i++)
            {
                var next = target.Cell + step;
                if (!IsFree(next)) break;
                target.Cell = next;
            }
            if (target.Cell != start)
                Emit(new FightEvent { Kind = FightEventKind.Pushed, Source = source, Target = target, Cell = target.Cell, Path = new List<Cell> { start, target.Cell } });
        }

        void Push(Fighter source, Fighter target, Cell awayFrom, int cells)
        {
            int dx = target.Cell.X - awayFrom.X;
            int dy = target.Cell.Y - awayFrom.Y;
            if (dx == 0 && dy == 0) return;
            var step = Math.Abs(dx) >= Math.Abs(dy) ? new Cell(Math.Sign(dx), 0) : new Cell(0, Math.Sign(dy));

            var start = target.Cell;
            int blocked = 0;
            for (int i = 0; i < cells; i++)
            {
                var next = target.Cell + step;
                if (!IsFree(next))
                {
                    blocked = cells - i;
                    break;
                }
                target.Cell = next;
            }
            if (target.Cell != start)
                Emit(new FightEvent { Kind = FightEventKind.Pushed, Source = source, Target = target, Cell = target.Cell, Path = new List<Cell> { start, target.Cell } });
            if (blocked > 0) Damage(source, target, blocked * (4 + source.Level / 2), "Colisão");
        }

        void CheckEnd()
        {
            bool playersAlive = false, monstersAlive = false;
            foreach (var f in Fighters)
            {
                if (!f.IsAlive) continue;
                if (f.Team == Team.Players) playersAlive = true;
                else monstersAlive = true;
            }
            if (playersAlive && monstersAlive) return;
            Phase = FightPhase.Ended;
            Winner = playersAlive ? Team.Players : Team.Monsters;
            Emit(new FightEvent { Kind = FightEventKind.FightEnded, Text = Winner.ToString() });
        }

        void Emit(FightEvent e) => Events.Enqueue(e);

        static string BuffLabel(BuffStat s) => s == BuffStat.Damage ? "dano" : s == BuffStat.Ap ? "PA" : "PM";
    }
}
