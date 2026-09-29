using System.Collections;
using System.Collections.Generic;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>
    /// Liga as regras da luta (Aldaria.Rules.Fight) ao que aparece na tela:
    /// transforma cliques em ações, anima os eventos em ordem e joga pelos monstros.
    /// </summary>
    public sealed class FightDirector : MonoBehaviour
    {
        public const float TurnTime = 30f;

        public Fight Fight { get; private set; }
        public Fighter Player { get; private set; }
        public SpellDef Selected { get; private set; }
        public float TurnTimeLeft { get; private set; }
        public readonly Dictionary<Fighter, Actor> Actors = new Dictionary<Fighter, Actor>();

        public bool Busy => busy || Fight.Events.Count > 0;
        public bool IsPlayerTurn => Fight.Phase == FightPhase.Fighting && Fight.Current == Player && !Busy;

        static readonly Color PlayersColor = new Color(0.25f, 0.55f, 1f, 0.9f);
        static readonly Color MonstersColor = new Color(1f, 0.3f, 0.25f, 0.9f);
        static readonly string[] Layers = { "place_p", "place_m", "reach", "path", "range", "range_off", "area" };

        GameController game;
        MonsterGroup group;
        bool busy, finished;
        float aiDelay;
        int aiActions;

        public void Begin(GameController g, MonsterGroup grp)
        {
            game = g;
            group = grp;
            var map = g.Map;
            Fight = new Fight(map, Random.Range(0, int.MaxValue));
            Player = Fighter.FromClass(1, g.Profile.Name, g.Profile.Class, g.Profile.Level, g.Profile.Hp);

            // Área de posicionamento de cada time: jogador perto de onde está, monstros a uma boa distância.
            var playerCells = NearestCells(map, g.Player.Cell, 5, null);
            var anchor = FindMonsterAnchor(map, playerCells[0], grp.Actor.Cell);
            var monsterCells = NearestCells(map, anchor, Mathf.Max(5, grp.Members.Count), new HashSet<Cell>(playerCells));
            Fight.PlacementCells[Team.Players].AddRange(playerCells);
            Fight.PlacementCells[Team.Monsters].AddRange(monsterCells);

            Fight.AddFighter(Player, playerCells[0]);
            Actors[Player] = g.Player;

            for (int i = 0; i < grp.Members.Count; i++)
            {
                var (def, level) = grp.Members[i];
                var f = Fighter.FromMonster(10 + i, def, level);
                Fight.AddFighter(f, monsterCells[i]);
                Actors[f] = i == 0 ? grp.Actor : Actor.Create(def.Name, Art.Character(def.Id), monsterCells[i], g.transform);
            }

            foreach (var kv in Actors)
            {
                kv.Value.Place(kv.Key.Cell);
                kv.Value.SetRing(true, kv.Key.Team == Team.Players ? PlayersColor : MonstersColor);
                kv.Value.FaceCell(kv.Key.Team == Team.Players ? anchor : playerCells[0]);
            }

            g.World.ShowGrid(true);
            g.AddLog("A luta vai começar! Escolha sua célula azul e clique em Pronto.", Art.Hex("#ffd166"));
        }

        public void Cleanup()
        {
            foreach (var l in Layers) game.World.ClearLayer(l);
            game.World.ShowGrid(false);
            foreach (var kv in Actors)
                if (kv.Value != game.Player && kv.Value != null) Destroy(kv.Value.gameObject);
            Actors.Clear();
        }

        // ================================================================== comandos do jogador

        public void Ready()
        {
            if (Fight.Phase != FightPhase.Placement) return;
            Fight.Start();
        }

        public void OnCellClicked(Cell? cell)
        {
            if (!cell.HasValue || Busy || finished) return;
            var c = cell.Value;

            if (Fight.Phase == FightPhase.Placement)
            {
                if (Fight.TryPlace(Player, c))
                    foreach (var kv in Actors) kv.Value.Place(kv.Key.Cell);
                return;
            }
            if (!IsPlayerTurn) return;

            if (Selected != null)
            {
                var problem = Fight.CastProblem(Player, Selected, c);
                if (problem == null)
                {
                    Fight.TryCast(Player, Selected, c);
                    Selected = null;
                }
                else
                {
                    game.AddLog(problem, new Color(1f, 0.7f, 0.6f));
                }
                return;
            }

            if (Fight.PathFor(Player, c) != null) Fight.TryMove(Player, c);
        }

        public string SpellProblem(SpellDef s)
        {
            if (Player.Ap < s.ApCost) return "PA insuficientes";
            int cd = Player.CooldownOf(s);
            if (cd > 0) return $"Recarregando ({cd})";
            if (Player.CastsOf(s) >= s.MaxCastsPerTurn) return "Limite de usos neste turno";
            return null;
        }

        public void SelectSpell(int index)
        {
            if (!IsPlayerTurn || index < 0 || index >= Player.Spells.Count) return;
            var s = Player.Spells[index];
            if (Selected == s)
            {
                Selected = null;
                return;
            }
            var problem = SpellProblem(s);
            if (problem != null)
            {
                game.AddLog($"{s.Name}: {problem}", new Color(1f, 0.7f, 0.6f));
                return;
            }
            if (s.Target == SpellTarget.Self)
            {
                Fight.TryCast(Player, s, Player.Cell);
                Selected = null;
                return;
            }
            Selected = s;
        }

        public void CancelSpell() => Selected = null;

        public void EndPlayerTurn()
        {
            if (!IsPlayerTurn) return;
            Selected = null;
            Fight.EndTurn();
        }

        // ================================================================== laço

        void Update()
        {
            if (finished) return;
            if (!busy && Fight.Events.Count > 0) StartCoroutine(ProcessEvents());

            if (!Busy && Fight.Phase == FightPhase.Fighting)
            {
                var cur = Fight.Current;
                if (cur.IsAI)
                {
                    aiDelay -= Time.deltaTime;
                    if (aiDelay <= 0f)
                    {
                        AiStep(cur);
                        aiDelay = 0.3f;
                    }
                }
                else if (cur == Player)
                {
                    TurnTimeLeft -= Time.deltaTime;
                    if (TurnTimeLeft <= 0f)
                    {
                        game.AddLog("Tempo esgotado!", new Color(1f, 0.7f, 0.6f));
                        EndPlayerTurn();
                    }
                }
            }
            UpdateOverlays();
        }

        void AiStep(Fighter me)
        {
            if (++aiActions > 12)
            {
                Fight.EndTurn();
                return;
            }
            var a = MonsterAI.Decide(Fight, me);
            bool ok = false;
            if (a.Kind == AiActionKind.Move) ok = Fight.TryMove(me, a.Target);
            else if (a.Kind == AiActionKind.Cast) ok = Fight.TryCast(me, a.Spell, a.Target);
            if (!ok) Fight.EndTurn();
        }

        void UpdateOverlays()
        {
            var w = game.World;
            var hover = game.PointerOverUi ? null : game.HoverCell;

            if (Fight.Phase == FightPhase.Placement)
            {
                w.SetLayer("place_p", Fight.PlacementCells[Team.Players], new Color(0.3f, 0.55f, 1f, 0.8f));
                w.SetLayer("place_m", Fight.PlacementCells[Team.Monsters], new Color(1f, 0.35f, 0.3f, 0.7f));
                return;
            }
            w.ClearLayer("place_p");
            w.ClearLayer("place_m");

            if (!IsPlayerTurn)
            {
                foreach (var l in new[] { "reach", "path", "range", "range_off", "area" }) w.ClearLayer(l);
                return;
            }

            if (Selected != null)
            {
                w.ClearLayer("reach");
                w.ClearLayer("path");
                var ok = new List<Cell>();
                var off = new List<Cell>();
                foreach (var c in Fight.RangeCells(Player, Selected))
                {
                    bool visible = !Selected.NeedsSight || Fight.HasSight(Player, Player.Cell, c);
                    (visible ? ok : off).Add(c);
                }
                w.SetLayer("range", ok, new Color(0.25f, 0.5f, 1f, 0.75f), 600);
                w.SetLayer("range_off", off, new Color(0.55f, 0.6f, 0.75f, 0.35f), 600);
                bool castable = hover.HasValue && Fight.CastProblem(Player, Selected, hover.Value) == null;
                w.SetLayer("area", castable ? Fight.AreaCells(Selected, hover.Value) : null, new Color(1f, 0.45f, 0.2f, 0.9f), 610);
            }
            else
            {
                w.ClearLayer("range");
                w.ClearLayer("range_off");
                w.ClearLayer("area");
                var reach = new List<Cell>();
                foreach (var kv in Fight.ReachableCells(Player))
                    if (kv.Value > 0) reach.Add(kv.Key);
                w.SetLayer("reach", reach, new Color(0.45f, 0.9f, 0.35f, 0.45f), 600);
                var path = hover.HasValue ? Fight.PathFor(Player, hover.Value) : null;
                w.SetLayer("path", path, new Color(0.55f, 1f, 0.4f, 0.95f), 610);
            }
        }

        // ================================================================== animação dos eventos

        IEnumerator ProcessEvents()
        {
            busy = true;
            while (Fight.Events.Count > 0)
            {
                var e = Fight.Events.Dequeue();
                yield return Animate(e);
                if (finished) break;
            }
            busy = false;
        }

        IEnumerator Animate(FightEvent e)
        {
            switch (e.Kind)
            {
                case FightEventKind.FightStarted:
                    game.AddLog("Que comece a luta!", Art.Hex("#ffd166"));
                    break;

                case FightEventKind.TurnStarted:
                    aiActions = 0;
                    aiDelay = 0.45f;
                    if (e.Source == Player)
                    {
                        TurnTimeLeft = TurnTime;
                        Selected = null;
                        game.AddLog($"Sua vez! {Player.Ap} PA, {Player.Mp} PM.", Art.Hex("#9ad0ff"));
                    }
                    Actors[e.Source].Flash(new Color(1f, 1f, 0.6f));
                    yield return new WaitForSeconds(0.15f);
                    break;

                case FightEventKind.Moved:
                {
                    var a = Actors[e.Source];
                    a.Walk(e.Path);
                    while (a.IsMoving) yield return null;
                    break;
                }

                case FightEventKind.SpellCast:
                {
                    var a = Actors[e.Source];
                    var target = Iso.ToWorld(e.Cell);
                    var color = Art.ElementColor(e.Spell.Element);
                    game.AddLog($"{e.Source.Name} lança {e.Spell.Name}.", Color.white);
                    if (e.Spell.Target == SpellTarget.Self)
                    {
                        Fx.Spawn(a.transform.position + Vector3.up * 0.35f, color, 0.5f, 2.2f, 0.5f);
                        yield return new WaitForSeconds(0.25f);
                        break;
                    }
                    StartCoroutine(a.Lunge(target));
                    if (e.Source.Cell.DistanceTo(e.Cell) > 1 && e.Spell.Effect != SpellEffect.Teleport)
                        yield return Projectile(a.transform.position + Vector3.up * 0.4f, target + Vector3.up * 0.3f, color);
                    else
                        yield return new WaitForSeconds(0.12f);

                    if (e.Spell.Area > 0)
                        foreach (var c in Fight.AreaCells(e.Spell, e.Cell))
                            Fx.Spawn(Iso.ToWorld(c) + Vector3.up * 0.1f, color, 0.6f, 2.4f, 0.45f);
                    else if (e.Spell.Effect != SpellEffect.Teleport)
                        Fx.Spawn(target + Vector3.up * 0.3f, color, 0.8f, 2.8f, 0.35f);
                    break;
                }

                case FightEventKind.Damaged:
                {
                    var a = Actors[e.Target];
                    a.Flash(new Color(1f, 0.25f, 0.2f));
                    game.AddPopup(a.HeadPosition, $"-{e.Value}", new Color(1f, 0.35f, 0.3f), true);
                    game.AddLog($"{e.Target.Name} perde {e.Value} PV.", new Color(1f, 0.75f, 0.7f));
                    yield return new WaitForSeconds(0.28f);
                    break;
                }

                case FightEventKind.Healed:
                {
                    var a = Actors[e.Target];
                    a.Flash(new Color(0.4f, 1f, 0.4f));
                    game.AddPopup(a.HeadPosition, $"+{e.Value}", new Color(0.45f, 1f, 0.45f), true);
                    game.AddLog($"{e.Target.Name} recupera {e.Value} PV.", new Color(0.7f, 1f, 0.7f));
                    yield return new WaitForSeconds(0.28f);
                    break;
                }

                case FightEventKind.Pushed:
                    yield return Actors[e.Target].SlideTo(e.Cell, 0.18f);
                    break;

                case FightEventKind.Teleported:
                    yield return Actors[e.Target].JumpTo(e.Cell, 0.4f);
                    Fx.Spawn(Iso.ToWorld(e.Cell) + Vector3.up * 0.1f, Art.ElementColor(Element.Air), 0.6f, 2.6f, 0.4f);
                    break;

                case FightEventKind.BuffApplied:
                    game.AddPopup(Actors[e.Target].HeadPosition, e.Text, new Color(0.75f, 0.6f, 1f));
                    game.AddLog($"{e.Target.Name}: {e.Text}", new Color(0.8f, 0.7f, 1f));
                    yield return new WaitForSeconds(0.2f);
                    break;

                case FightEventKind.Died:
                {
                    game.AddLog($"{e.Target.Name} foi derrotado!", Art.Hex("#ffb74d"));
                    var a = Actors[e.Target];
                    a.SetRing(false, Color.white);
                    yield return a.FadeOut(0.5f);
                    break;
                }

                case FightEventKind.FightEnded:
                    yield return new WaitForSeconds(0.6f);
                    Finish();
                    break;
            }
        }

        IEnumerator Projectile(Vector3 from, Vector3 to, Color color)
        {
            var fx = Fx.Spawn(from, color, 0.9f, 0.9f, 10f);
            float dist = Vector3.Distance(from, to);
            float duration = Mathf.Clamp(dist * 0.07f, 0.12f, 0.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                fx.transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * dist * 0.12f;
                yield return null;
            }
            Destroy(fx.gameObject);
        }

        void Finish()
        {
            finished = true;
            foreach (var l in Layers) game.World.ClearLayer(l);

            var profile = game.Profile;
            var result = new FightResult { Victory = Fight.Winner == Team.Players };
            var monsters = Fight.Fighters.FindAll(f => f.Team == Team.Monsters);
            if (result.Victory)
            {
                result.Xp = Progression.XpReward(monsters, profile.Level);
                result.Gold = Progression.GoldReward(monsters, new System.Random());
                profile.Hp = Mathf.Max(1, Player.Hp);
                profile.Gold += result.Gold;
                profile.Victories++;
                result.LevelsGained = profile.GainXp(result.Xp);
                game.AddLog($"Vitória! +{result.Xp} XP, +{result.Gold} de ouro.", Art.Hex("#ffd166"));
                if (result.LevelsGained > 0) game.AddLog($"Você subiu para o nível {profile.Level}!", Art.Hex("#ffd166"));
            }
            else
            {
                game.AddLog("Derrota...", Art.Hex("#ff8a80"));
            }
            result.NewLevel = profile.Level;
            game.OnFightFinished(result);
        }

        static List<Cell> NearestCells(GridMap map, Cell start, int count, HashSet<Cell> exclude)
        {
            var result = new List<Cell>();
            var dist = Pathfinder.Reachable(start, int.MaxValue, map.IsWalkable);
            var cells = new List<Cell>(dist.Keys);
            cells.Sort((a, b) => dist[a].CompareTo(dist[b]));
            foreach (var c in cells)
            {
                if (!map.IsWalkable(c) || (exclude != null && exclude.Contains(c))) continue;
                result.Add(c);
                if (result.Count >= count) break;
            }
            return result;
        }

        static Cell FindMonsterAnchor(GridMap map, Cell playerCell, Cell groupCell)
        {
            var dist = Pathfinder.Reachable(playerCell, int.MaxValue, map.IsWalkable);
            Cell best = groupCell;
            int bestScore = int.MaxValue;
            Cell farthest = groupCell;
            int farthestD = -1;
            foreach (var kv in dist)
            {
                int d = kv.Key.DistanceTo(playerCell);
                if (d > farthestD)
                {
                    farthestD = d;
                    farthest = kv.Key;
                }
                if (d < 6 || d > 9) continue;
                int score = kv.Key.DistanceTo(groupCell);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = kv.Key;
                }
            }
            return bestScore == int.MaxValue ? farthest : best;
        }
    }
}
