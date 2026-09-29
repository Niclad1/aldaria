using System;
using System.Collections;
using System.Collections.Generic;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    public enum GameMode
    {
        Title,
        Exploration,
        Fight
    }

    public enum UiPanel
    {
        None,
        Inventory,
        Quests,
        Dialogue,
        Shop
    }

    public sealed class MonsterGroup
    {
        public readonly List<(MonsterDef def, int level)> Members = new List<(MonsterDef, int)>();
        public Actor Actor;
        public float NextWander;
        public bool IsBoss;

        public int TotalLevel
        {
            get
            {
                int n = 0;
                foreach (var m in Members) n += m.level;
                return n;
            }
        }
    }

    public sealed class NpcInstance
    {
        public NpcDef Def;
        public Actor Actor;
    }

    public sealed class FightResult
    {
        public bool Victory;
        public int Xp;
        public int Gold;
        public int LevelsGained;
        public int NewLevel;
        public List<ItemStack> Drops = new List<ItemStack>();
    }

    public struct LogLine
    {
        public string Text;
        public Color Color;
    }

    public sealed class Popup
    {
        public Vector3 World;
        public string Text;
        public Color Color;
        public float Age;
        public bool Big;
    }

    /// <summary>
    /// Ponto de entrada do jogo. Cria tudo por código quando você aperta Play em qualquer cena,
    /// cuida da exploração (andar, trocar de mapa, NPCs, grupos de monstros) e passa o controle
    /// para o FightDirector quando uma luta começa.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        public GameMode Mode { get; private set; } = GameMode.Title;
        public PlayerProfile Profile { get; private set; }
        public PlayerProfile SavedProfile { get; private set; }
        public GridMap Map { get; private set; }
        public WorldView World { get; private set; }
        public Actor Player { get; private set; }
        public FightDirector Fight { get; private set; }
        public Camera Cam { get; private set; }
        public FightResult LastResult { get; private set; }
        public Cell? HoverCell { get; private set; }
        public MonsterGroup HoverGroup { get; private set; }
        public NpcInstance HoverNpc { get; private set; }
        public Vector2 MouseWorld { get; private set; }
        public float FadeAlpha { get; private set; }
        /// <summary>Faixa grande no centro da tela ("Sua vez!").</summary>
        public string BannerText { get; private set; }
        public Color BannerColor { get; private set; }
        public float BannerAge { get; private set; } = 99f;

        public void Banner(string text, Color color)
        {
            BannerText = text;
            BannerColor = color;
            BannerAge = 0f;
        }

        public UiPanel OpenPanel { get; private set; }
        public NpcDef DialogueNpc { get; private set; }
        /// <summary>Missão aberta na conversa (oferta, andamento ou entrega); null = menu inicial do NPC.</summary>
        public QuestDef DialogueQuest { get; set; }

        public readonly List<MonsterGroup> Groups = new List<MonsterGroup>();
        public readonly List<NpcInstance> Npcs = new List<NpcInstance>();
        public readonly List<LogLine> Log = new List<LogLine>();
        public readonly List<Popup> Popups = new List<Popup>();

        /// <summary>Posição do mouse em pixels de tela (preenchida pelo Hud, funciona com qualquer sistema de input).</summary>
        public Vector2 ScreenMouse { get; set; }
        public bool PointerOverUi { get; set; }

        MonsterGroup targetGroup;
        NpcInstance targetNpc;
        MonsterGroup fightGroup;
        int approachTries;
        float zoom = 5f, maxZoom = 5f, lastAspect;
        float regen;
        float respawnAt = -1f;
        bool changingMap;
        const float MinZoom = 2.4f;

        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        static readonly Color Info = new Color(0.6f, 0.82f, 1f);
        static readonly Color Warn = new Color(1f, 0.7f, 0.6f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Aldaria");
            go.AddComponent<GameController>();
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            SetupCamera();
            gameObject.AddComponent<Hud>();
            SavedProfile = SaveSystem.Load();
            SavedProfile?.Validate();
            LoadMap(SavedProfile?.MapX ?? 0, SavedProfile?.MapY ?? 0, null);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() => Save();

        // ================================================================== fluxo principal

        public void StartGame(PlayerProfile profile)
        {
            Profile = profile;
            Profile.Validate();
            SavedProfile = profile;
            Mode = GameMode.Exploration;
            OpenPanel = UiPanel.None;
            Log.Clear();
            var entry = Profile.CellX >= 0 ? new Cell(Profile.CellX, Profile.CellY) : (Cell?)null;
            LoadMap(Profile.MapX, Profile.MapY, entry);
            AddLog($"Bem-vindo a Aldaria, {Profile.Name}!", Gold);
            AddLog("Fale com os moradores (clique neles). Quem tem <b>!</b> na cabeça tem uma missão para você.", Color.white);
            AddLog("I = inventário, J = missões. Clique num grupo de monstros para lutar.", Color.white);
            Save();
        }

        public void BackToTitle()
        {
            if (Mode == GameMode.Fight) return;
            Save();
            Mode = GameMode.Title;
            OpenPanel = UiPanel.None;
            if (Player != null) Destroy(Player.gameObject);
            Player = null;
            targetGroup = null;
            targetNpc = null;
            if (World != null) World.Focus = null;
            FitCamera();
        }

        public void Save()
        {
            if (Profile == null) return;
            if (Player != null && Mode != GameMode.Fight)
            {
                Profile.CellX = Player.Cell.X;
                Profile.CellY = Player.Cell.Y;
            }
            SaveSystem.Save(Profile);
        }

        void LoadMap(int mapX, int mapY, Cell? entry)
        {
            foreach (var g in Groups)
                if (g.Actor != null) Destroy(g.Actor.gameObject);
            Groups.Clear();
            foreach (var n in Npcs)
                if (n.Actor != null) Destroy(n.Actor.gameObject);
            Npcs.Clear();
            targetGroup = null;
            targetNpc = null;
            if (World != null) Destroy(World.gameObject);

            Map = MapGenerator.Generate(mapX, mapY);
            World = WorldView.Build(Map, transform);

            foreach (var def in Catalog.NpcsAt(mapX, mapY))
            {
                var a = Actor.Create(def.Name, Art.Character("npc_" + def.Id), def.Cell, transform);
                a.Face(Vector3.left);
                Npcs.Add(new NpcInstance { Def = def, Actor = a });
            }

            if (Mode != GameMode.Title && Profile != null)
            {
                var cell = entry.HasValue && IsPassable(entry.Value) ? entry.Value : NearestWalkable(entry ?? Map.Center);
                if (Player == null)
                {
                    Player = Actor.Create(Profile.Name, Art.Character(Profile.ClassId), cell, transform);
                    var walk = Art.Get("char_" + Profile.ClassId + "_walk");
                    if (walk != null) Player.SetWalkSprite(walk);
                }
                else Player.Place(cell);
                Player.Speed = 4.2f;
                Profile.MapX = mapX;
                Profile.MapY = mapY;
                World.Focus = Player.transform;
                QuestLog.OnVisit(Profile, mapX, mapY);
            }

            if (mapX == Catalog.BossMapX && mapY == Catalog.BossMapY) SpawnBoss();
            int groups = Catalog.MonsterPool(Map.Region).Length == 0 ? 0 : UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < groups; i++) SpawnGroup();
            FitCamera();
        }

        void SpawnGroup()
        {
            var pool = Catalog.MonsterPool(Map.Region);
            if (pool.Length == 0) return;
            Catalog.LevelRange(Map.MapX, Map.MapY, out int min, out int max);
            var group = new MonsterGroup();
            int count = UnityEngine.Random.value < 0.45f ? 1 : UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                var def = Catalog.MonsterById(pool[UnityEngine.Random.Range(0, pool.Length)]);
                group.Members.Add((def, UnityEngine.Random.Range(min, max + 1)));
            }
            PlaceGroup(group, 5);
        }

        void SpawnBoss()
        {
            var group = new MonsterGroup { IsBoss = true };
            group.Members.Add((Catalog.MonsterById("rei_lanudo"), 14));
            group.Members.Add((Catalog.MonsterById("lanudo"), 11));
            group.Members.Add((Catalog.MonsterById("lanudo"), 11));
            PlaceGroup(group, 4);
            if (group.Actor != null) group.Actor.SetScale(1.1f);
        }

        void PlaceGroup(MonsterGroup group, int minDistance)
        {
            var candidates = new List<Cell>();
            foreach (var c in Map.Cells())
            {
                if (!IsPassable(c) || Map.Exits.Contains(c) || GroupAt(c) != null) continue;
                if (Player != null && c.DistanceTo(Player.Cell) < minDistance) continue;
                candidates.Add(c);
            }
            if (candidates.Count == 0) return;
            var cell = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            group.Actor = Actor.Create($"Grupo: {group.Members[0].def.Name}", Art.Character(group.Members[0].def.Id), cell, transform);
            group.Actor.Speed = 2.2f;
            group.NextWander = Time.time + UnityEngine.Random.Range(1f, 5f);
            Groups.Add(group);
        }

        // ================================================================== atualização

        void Update()
        {
            UpdateHover();

            if (Mode != GameMode.Fight) WanderGroups();
            if (Mode == GameMode.Exploration)
            {
                RegenerateHp();
                if (respawnAt > 0f && Time.time > respawnAt)
                {
                    respawnAt = -1f;
                    SpawnGroup();
                }
                bool showHover = HoverCell.HasValue && !PointerOverUi && LastResult == null && IsPassable(HoverCell.Value);
                World.SetLayer("hover", showHover ? new[] { HoverCell.Value } : null, new Color(1f, 1f, 1f, 0.35f), 640);
            }
            else
            {
                World.ClearLayer("hover");
            }
            World.SetExitsVisible(Mode != GameMode.Fight);

            BannerAge += Time.deltaTime;
            for (int i = Popups.Count - 1; i >= 0; i--)
            {
                Popups[i].Age += Time.deltaTime;
                if (Popups[i].Age > 1.3f) Popups.RemoveAt(i);
            }
            UpdateCamera();
        }

        void UpdateHover()
        {
            if (Cam == null) return;
            var sp = new Vector3(ScreenMouse.x, ScreenMouse.y, -Cam.transform.position.z);
            MouseWorld = Cam.ScreenToWorldPoint(sp);
            var cell = Iso.ToCell(MouseWorld);
            HoverCell = Map.InBounds(cell) ? cell : (Cell?)null;

            HoverGroup = null;
            HoverNpc = null;
            if (Mode == GameMode.Exploration && !PointerOverUi)
            {
                foreach (var g in Groups)
                    if (g.Actor.HitTest(MouseWorld) || (HoverCell.HasValue && g.Actor.Cell == HoverCell.Value))
                        HoverGroup = g;
                foreach (var n in Npcs)
                    if (n.Actor.HitTest(MouseWorld) || (HoverCell.HasValue && n.Def.Cell == HoverCell.Value))
                        HoverNpc = n;
            }
        }

        void WanderGroups()
        {
            foreach (var g in Groups)
            {
                if (g == targetGroup || g.Actor.IsMoving || Time.time < g.NextWander) continue;
                g.NextWander = Time.time + UnityEngine.Random.Range(3f, 7f);
                var options = new List<Cell>();
                foreach (var kv in Pathfinder.Reachable(g.Actor.Cell, 2, IsPassable))
                {
                    var c = kv.Key;
                    if (kv.Value == 0 || Map.Exits.Contains(c) || GroupAt(c) != null) continue;
                    if (Player != null && c.DistanceTo(Player.Destination) < 2) continue;
                    options.Add(c);
                }
                if (options.Count == 0) continue;
                var path = Pathfinder.FindPath(g.Actor.Cell, options[UnityEngine.Random.Range(0, options.Count)], IsPassable);
                if (path != null) g.Actor.Walk(path);
            }
        }

        void RegenerateHp()
        {
            if (Profile == null || Profile.Hp >= Profile.MaxHp) return;
            regen += Time.deltaTime * Mathf.Max(1f, Profile.MaxHp * 0.02f);
            int gained = Mathf.FloorToInt(regen);
            if (gained <= 0) return;
            regen -= gained;
            Profile.Hp = Mathf.Min(Profile.MaxHp, Profile.Hp + gained);
        }

        // ================================================================== entrada do jogador

        public void OnWorldClick()
        {
            if (Mode == GameMode.Fight)
            {
                Fight.OnCellClicked(HoverCell);
                return;
            }
            if (Mode != GameMode.Exploration || LastResult != null || changingMap || !HoverCell.HasValue) return;
            if (OpenPanel == UiPanel.Dialogue || OpenPanel == UiPanel.Shop) ClosePanel();

            if (HoverNpc != null)
            {
                targetGroup = null;
                targetNpc = HoverNpc;
                ApproachNpc();
                return;
            }
            if (HoverGroup != null)
            {
                targetNpc = null;
                targetGroup = HoverGroup;
                approachTries = 0;
                ApproachGroup();
                return;
            }

            targetGroup = null;
            targetNpc = null;
            var cell = HoverCell.Value;
            if (!IsPassable(cell)) return;
            bool isExit = Map.Exits.Contains(cell);
            WalkPlayer(cell, isExit ? () => StartCoroutine(UseExit(cell)) : (Action)null);
        }

        public void OnRightClick()
        {
            if (Mode == GameMode.Fight) Fight.CancelSpell();
        }

        public void OnKey(KeyCode key)
        {
            if (Mode == GameMode.Exploration && LastResult == null)
            {
                switch (key)
                {
                    case KeyCode.I: TogglePanel(UiPanel.Inventory); break;
                    case KeyCode.J: case KeyCode.Q: TogglePanel(UiPanel.Quests); break;
                    case KeyCode.Escape: ClosePanel(); break;
                }
                return;
            }
            if (Mode != GameMode.Fight || Fight == null) return;
            switch (key)
            {
                case KeyCode.Alpha1: case KeyCode.Keypad1: Fight.SelectSpell(0); break;
                case KeyCode.Alpha2: case KeyCode.Keypad2: Fight.SelectSpell(1); break;
                case KeyCode.Alpha3: case KeyCode.Keypad3: Fight.SelectSpell(2); break;
                case KeyCode.Alpha4: case KeyCode.Keypad4: Fight.SelectSpell(3); break;
                case KeyCode.Escape: Fight.CancelSpell(); break;
                case KeyCode.Space:
                case KeyCode.Return:
                    if (Fight.Fight.Phase == FightPhase.Placement) Fight.Ready();
                    else Fight.EndPlayerTurn();
                    break;
            }
        }

        public void Zoom(float delta)
        {
            zoom = Mathf.Clamp(zoom + delta * 0.2f, MinZoom, maxZoom);
        }

        public bool IsPassable(Cell c)
        {
            if (!Map.IsWalkable(c)) return false;
            foreach (var n in Npcs)
                if (n.Def.Cell == c) return false;
            return true;
        }

        bool WalkPlayer(Cell target, Action onArrive)
        {
            var path = Pathfinder.FindPath(Player.PlanningCell, target, IsPassable);
            if (path == null) return false;
            Player.Walk(path, onArrive);
            return true;
        }

        List<Cell> PathNextTo(Cell target)
        {
            List<Cell> best = null;
            if (Player.PlanningCell.DistanceTo(target) == 1) return new List<Cell>();
            foreach (var n in target.Neighbors())
            {
                if (!IsPassable(n)) continue;
                var path = Pathfinder.FindPath(Player.PlanningCell, n, IsPassable);
                if (path != null && (best == null || path.Count < best.Count)) best = path;
            }
            return best;
        }

        void ApproachNpc()
        {
            var npc = targetNpc;
            var path = PathNextTo(npc.Def.Cell);
            if (path == null)
            {
                targetNpc = null;
                return;
            }
            Player.Walk(path, () =>
            {
                if (targetNpc != npc) return;
                targetNpc = null;
                Player.FaceCell(npc.Def.Cell);
                npc.Actor.FaceCell(Player.Cell);
                OpenDialogue(npc.Def);
            });
        }

        void ApproachGroup()
        {
            var g = targetGroup;
            if (g == null || !Groups.Contains(g)) return;
            var gc = g.Actor.Destination;
            if (!Player.IsMoving && Player.Cell.DistanceTo(gc) <= 1)
            {
                StartFight(g);
                return;
            }
            var best = PathNextTo(gc);
            if (best == null)
            {
                targetGroup = null;
                return;
            }
            Player.Walk(best, () =>
            {
                if (targetGroup != g) return;
                if (Player.Cell.DistanceTo(g.Actor.Destination) <= 1) StartFight(g);
                else if (++approachTries < 5) ApproachGroup();
                else targetGroup = null;
            });
        }

        IEnumerator UseExit(Cell exit)
        {
            if (changingMap) yield break;
            changingMap = true;
            ClosePanel();
            var side = MapGenerator.SideOf(exit);
            if (!side.HasValue)
            {
                changingMap = false;
                yield break;
            }
            MapGenerator.Neighbor(Map.MapX, Map.MapY, side.Value, out int mx, out int my);
            var entry = MapGenerator.ExitCell(MapGenerator.Opposite(side.Value));

            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                FadeAlpha = t;
                yield return null;
            }
            FadeAlpha = 1f;
            var before = QuestSnapshot();
            LoadMap(mx, my, entry);
            // Sai um passo da borda para não ficar em cima da saída.
            var step = Pathfinder.FindPath(entry, Map.Center, IsPassable);
            if (step != null && step.Count > 0) Player.Walk(new List<Cell> { step[0] });
            AddLog($"Você chegou em {Map.Name} [{Map.MapX},{Map.MapY}] — {Catalog.RegionName(Map.Region)}.", Info);
            if (Map.MapX == Catalog.BossMapX && Map.MapY == Catalog.BossMapY) AddLog("Você sente o chão tremer... o Rei Lanudo está aqui!", Warn);
            ReportQuestChanges(before);
            Save();
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
            {
                FadeAlpha = 1f - t;
                yield return null;
            }
            FadeAlpha = 0f;
            changingMap = false;
        }

        // ================================================================== painéis, NPCs e itens

        public void TogglePanel(UiPanel p)
        {
            if (Mode != GameMode.Exploration) return;
            OpenPanel = OpenPanel == p ? UiPanel.None : p;
            if (OpenPanel != UiPanel.Dialogue && OpenPanel != UiPanel.Shop) DialogueNpc = null;
        }

        public void ClosePanel()
        {
            OpenPanel = UiPanel.None;
            DialogueNpc = null;
            DialogueQuest = null;
        }

        public void OpenDialogue(NpcDef npc)
        {
            var before = QuestSnapshot();
            QuestLog.OnTalk(Profile, npc.Id);
            ReportQuestChanges(before);
            DialogueNpc = npc;
            DialogueQuest = null;
            OpenPanel = UiPanel.Dialogue;
        }

        public void OpenShop()
        {
            if (DialogueNpc != null && DialogueNpc.IsShop) OpenPanel = UiPanel.Shop;
        }

        /// <summary>Ícone sobre a cabeça do NPC: "!" missão nova, "?" missão para entregar, "…" missão em andamento.</summary>
        public string NpcMarker(NpcDef npc)
        {
            if (Profile == null) return null;
            string marker = null;
            foreach (var q in Catalog.Quests)
            {
                var st = QuestLog.StatusOf(Profile, q);
                if (st == QuestStatus.ReadyToTurnIn && q.TurnIn == npc.Id) return "?";
                if (st == QuestStatus.Available && q.Giver == npc.Id) marker = "!";
                if (st == QuestStatus.Active && marker == null && (q.Giver == npc.Id || q.TurnIn == npc.Id)) marker = "…";
            }
            return marker;
        }

        public void AcceptQuest(QuestDef q)
        {
            if (!QuestLog.Accept(Profile, q)) return;
            AddLog($"Nova missão: <b>{q.Name}</b>", Gold);
            // Objetivos de "falar" ou "visitar" podem já estar cumpridos.
            if (DialogueNpc != null) QuestLog.OnTalk(Profile, DialogueNpc.Id);
            QuestLog.OnVisit(Profile, Map.MapX, Map.MapY);
            DialogueQuest = null;
            Save();
        }

        public void TurnInQuest(QuestDef q)
        {
            int levels = QuestLog.TurnIn(Profile, q);
            if (levels < 0) return;
            var rewards = new List<string>();
            if (q.RewardXp > 0) rewards.Add($"+{q.RewardXp} XP");
            if (q.RewardGold > 0) rewards.Add($"+{q.RewardGold} ouro");
            foreach (var it in q.RewardItems) rewards.Add($"{it.Count}x {Catalog.ItemById(it.Id).Name}");
            AddLog($"Missão concluída: <b>{q.Name}</b>! {string.Join(", ", rewards)}", Gold);
            if (levels > 0) LevelUp();
            Popup(Player.HeadPosition, "Missão concluída!", Gold, true);
            DialogueQuest = null;
            Save();
        }

        public void EquipItem(string id)
        {
            var problem = Profile.Equip(id);
            if (problem != null) AddLog(problem, Warn);
            else AddLog($"Equipado: {Catalog.ItemById(id).Name}", Info);
            Save();
        }

        public void UnequipSlot(ItemSlot slot)
        {
            Profile.Unequip(slot);
            Save();
        }

        public void UseItem(string id)
        {
            int hp = Profile.Hp;
            var problem = Profile.Use(id);
            if (problem != null)
            {
                AddLog(problem, Warn);
                return;
            }
            Popup(Player.HeadPosition, $"+{Profile.Hp - hp}", new Color(0.45f, 1f, 0.45f), true);
            Save();
        }

        public void BuyItem(string id)
        {
            var problem = Profile.Buy(id);
            if (problem != null) AddLog(problem, Warn);
            else AddLog($"Comprou {Catalog.ItemById(id).Name}.", Info);
            Save();
        }

        public void SellItem(string id)
        {
            var it = Catalog.ItemById(id);
            var problem = Profile.Sell(id);
            if (problem != null) AddLog(problem, Warn);
            else AddLog($"Vendeu {it.Name} por {it.SellPrice} ouro.", Info);
            Save();
        }

        List<QuestStatus> QuestSnapshot()
        {
            var list = new List<QuestStatus>();
            if (Profile == null) return list;
            foreach (var q in Catalog.Quests) list.Add(QuestLog.StatusOf(Profile, q));
            return list;
        }

        void ReportQuestChanges(List<QuestStatus> before)
        {
            if (Profile == null || before.Count != Catalog.Quests.Count) return;
            for (int i = 0; i < Catalog.Quests.Count; i++)
            {
                var q = Catalog.Quests[i];
                if (before[i] != QuestStatus.ReadyToTurnIn && QuestLog.StatusOf(Profile, q) == QuestStatus.ReadyToTurnIn)
                    AddLog($"Missão pronta: <b>{q.Name}</b> — fale com {Catalog.NpcById(q.TurnIn).Name}.", Gold);
            }
        }

        void LevelUp()
        {
            AddLog($"Você subiu para o nível {Profile.Level}!", Gold);
            if (Player != null)
            {
                Popup(Player.HeadPosition + Vector3.up * 0.2f, $"Nível {Profile.Level}!", Gold, true);
                Fx.Spawn(Player.transform.position + Vector3.up * 0.4f, Gold, 0.5f, 3f, 0.8f);
            }
        }

        // ================================================================== luta

        void StartFight(MonsterGroup group)
        {
            if (Mode != GameMode.Exploration) return;
            ClosePanel();
            Mode = GameMode.Fight;
            targetGroup = null;
            fightGroup = group;
            Player.Stop();
            group.Actor.Stop();
            foreach (var g in Groups)
                if (g != group) g.Actor.gameObject.SetActive(false);
            foreach (var n in Npcs) n.Actor.gameObject.SetActive(false);
            Fight = gameObject.AddComponent<FightDirector>();
            Fight.Begin(this, group);
        }

        /// <summary>Chamado pelo FightDirector: aplica recompensas, missões e drops.</summary>
        public void OnFightFinished(FightResult result, List<Fighter> monsters, int playerHp)
        {
            if (result.Victory)
            {
                var before = QuestSnapshot();
                Profile.Hp = Mathf.Max(1, playerHp);
                Profile.Gold += result.Gold;
                Profile.Victories++;
                foreach (var d in result.Drops) Profile.AddItem(d.Id, d.Count);
                foreach (var m in monsters) QuestLog.OnKill(Profile, m.Visual);
                result.LevelsGained = Profile.GainXp(result.Xp);
                AddLog($"Vitória! +{result.Xp} XP, +{result.Gold} ouro.", Gold);
                foreach (var d in result.Drops) AddLog($"Você pegou {d.Count}x {Catalog.ItemById(d.Id).Name}.", Info);
                ReportQuestChanges(before);
                if (result.LevelsGained > 0) LevelUp();
            }
            else
            {
                AddLog("Derrota...", Warn);
            }
            result.NewLevel = Profile.Level;
            LastResult = result;
            Save();
        }

        public void CloseFightResult()
        {
            if (LastResult == null) return;
            bool victory = LastResult.Victory;
            LastResult = null;

            Fight.Cleanup();
            Destroy(Fight);
            Fight = null;
            Groups.Remove(fightGroup);
            fightGroup = null;
            Player.ResetVisual();
            Mode = GameMode.Exploration;

            foreach (var g in Groups) g.Actor.gameObject.SetActive(true);
            foreach (var n in Npcs) n.Actor.gameObject.SetActive(true);
            if (victory)
            {
                respawnAt = Time.time + 20f;
            }
            else
            {
                Profile.Hp = Mathf.Max(1, Profile.MaxHp / 2);
                LoadMap(0, 0, null);
                AddLog("Você desmaiou e acordou na Vila de Aldaria.", Warn);
            }
            Save();
        }

        // ================================================================== utilidades

        public MonsterGroup GroupAt(Cell c)
        {
            foreach (var g in Groups)
                if (g.Actor != null && (g.Actor.Cell == c || g.Actor.Destination == c)) return g;
            return null;
        }

        Cell NearestWalkable(Cell from)
        {
            Cell best = Map.Center;
            int bestD = int.MaxValue;
            foreach (var c in Map.Cells())
            {
                if (!IsPassable(c)) continue;
                int d = c.DistanceTo(from);
                if (d < bestD)
                {
                    bestD = d;
                    best = c;
                }
            }
            return best;
        }

        public void AddLog(string text, Color color)
        {
            Log.Add(new LogLine { Text = text, Color = color });
            if (Log.Count > 60) Log.RemoveAt(0);
        }

        public void Popup(Vector3 world, string text, Color color, bool big = false)
        {
            Popups.Add(new Popup { World = world, Text = text, Color = color, Big = big });
        }

        // ================================================================== câmera

        void SetupCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = go.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Art.Hex("#1b2622");
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 100f;
            Cam.transform.rotation = Quaternion.identity;
            Cam.transform.position = new Vector3(0f, -8f, -10f);
        }

        void FitCamera()
        {
            if (Cam == null || Map == null) return;
            // O mapa é um retângulo de ~15 x 8,5 unidades; sobra uma margem para árvores e barrancos.
            float halfW = MapGenerator.HalfWidth * 0.5f + 0.5f;
            float halfH = MapGenerator.HalfHeight * 0.25f + 1.1f;
            maxZoom = Mathf.Max(halfH, halfW / Mathf.Max(0.5f, Cam.aspect));
            zoom = maxZoom;
            lastAspect = Cam.aspect;
            Cam.orthographicSize = zoom;
        }

        void UpdateCamera()
        {
            if (Cam == null || Map == null) return;
            if (Mathf.Abs(Cam.aspect - lastAspect) > 0.001f) FitCamera();
            Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, zoom, Time.deltaTime * 8f);

            var center = Iso.ToWorld(Map.Center) + new Vector3(0f, -0.2f, 0f);
            var target = center;
            if (Player != null && maxZoom > MinZoom)
            {
                float t = 1f - (zoom - MinZoom) / (maxZoom - MinZoom);
                target = Vector3.Lerp(center, Player.transform.position, t);
            }
            var pos = Vector3.Lerp(Cam.transform.position, target, Time.deltaTime * 6f);
            Cam.transform.position = new Vector3(pos.x, pos.y, -10f);
        }
    }
}
