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

    public sealed class MonsterGroup
    {
        public readonly List<(MonsterDef def, int level)> Members = new List<(MonsterDef, int)>();
        public Actor Actor;
        public float NextWander;

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

    public sealed class FightResult
    {
        public bool Victory;
        public int Xp;
        public int Gold;
        public int LevelsGained;
        public int NewLevel;
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
    /// cuida da exploração (andar, trocar de mapa, grupos de monstros) e passa o controle
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
        public Vector2 MouseWorld { get; private set; }
        public float FadeAlpha { get; private set; }

        public readonly List<MonsterGroup> Groups = new List<MonsterGroup>();
        public readonly List<LogLine> Log = new List<LogLine>();
        public readonly List<Popup> Popups = new List<Popup>();

        /// <summary>Posição do mouse em pixels de tela (preenchida pelo Hud, funciona com qualquer sistema de input).</summary>
        public Vector2 ScreenMouse { get; set; }
        public bool PointerOverUi { get; set; }

        MonsterGroup targetGroup;
        MonsterGroup fightGroup;
        int approachTries;
        float zoom = 5f, maxZoom = 5f, lastAspect;
        float regen;
        float respawnAt = -1f;
        bool changingMap;
        const float MinZoom = 2.6f;

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
            if (Profile.Hp <= 0 || Profile.Hp > Profile.MaxHp) Profile.Hp = Profile.MaxHp;
            SavedProfile = profile;
            Mode = GameMode.Exploration;
            Log.Clear();
            var entry = Profile.CellX >= 0 ? new Cell(Profile.CellX, Profile.CellY) : (Cell?)null;
            LoadMap(Profile.MapX, Profile.MapY, entry);
            AddLog($"Bem-vindo a Aldaria, {Profile.Name}!", Art.Hex("#ffd166"));
            AddLog("Clique no chão para andar. Clique num grupo de monstros para lutar.", Color.white);
            Save();
        }

        public void BackToTitle()
        {
            if (Mode == GameMode.Fight) return;
            Save();
            Mode = GameMode.Title;
            if (Player != null) Destroy(Player.gameObject);
            Player = null;
            targetGroup = null;
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
            targetGroup = null;
            if (World != null) Destroy(World.gameObject);

            Map = MapGenerator.Generate(mapX, mapY);
            World = WorldView.Build(Map, transform);

            if (Mode != GameMode.Title && Profile != null)
            {
                var cell = entry.HasValue && Map.IsWalkable(entry.Value) ? entry.Value : NearestWalkable(entry ?? Map.Center);
                if (Player == null) Player = Actor.Create(Profile.Name, Art.Character(Profile.ClassId), cell, transform);
                else Player.Place(cell);
                Player.Speed = 4.2f;
                Profile.MapX = mapX;
                Profile.MapY = mapY;
                World.Focus = Player.transform;
            }

            int groups = UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < groups; i++) SpawnGroup();
            FitCamera();
        }

        void SpawnGroup()
        {
            int distance = Mathf.Abs(Map.MapX) + Mathf.Abs(Map.MapY);
            int baseLevel = 1 + distance * 2;
            var group = new MonsterGroup();
            int count = UnityEngine.Random.value < 0.45f ? 1 : UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                var def = Catalog.Monsters[UnityEngine.Random.Range(0, Catalog.Monsters.Count)];
                group.Members.Add((def, Mathf.Max(1, baseLevel + UnityEngine.Random.Range(-1, 2))));
            }

            var candidates = new List<Cell>();
            foreach (var c in Map.Cells())
            {
                if (!Map.IsWalkable(c) || Map.Exits.Contains(c) || GroupAt(c) != null) continue;
                if (Player != null && c.DistanceTo(Player.Cell) < 5) continue;
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
                bool showHover = HoverCell.HasValue && !PointerOverUi && LastResult == null && Map.IsWalkable(HoverCell.Value);
                World.SetLayer("hover", showHover ? new[] { HoverCell.Value } : null, new Color(1f, 1f, 1f, 0.35f), 640);
            }
            else
            {
                World.ClearLayer("hover");
            }
            World.SetExitsVisible(Mode != GameMode.Fight);

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
            if (Mode == GameMode.Exploration && !PointerOverUi)
            {
                foreach (var g in Groups)
                    if (g.Actor.HitTest(MouseWorld) || (HoverCell.HasValue && g.Actor.Cell == HoverCell.Value))
                        HoverGroup = g;
            }
        }

        void WanderGroups()
        {
            foreach (var g in Groups)
            {
                if (g == targetGroup || g.Actor.IsMoving || Time.time < g.NextWander) continue;
                g.NextWander = Time.time + UnityEngine.Random.Range(3f, 7f);
                var options = new List<Cell>();
                foreach (var kv in Pathfinder.Reachable(g.Actor.Cell, 2, Map.IsWalkable))
                {
                    var c = kv.Key;
                    if (kv.Value == 0 || Map.Exits.Contains(c) || GroupAt(c) != null) continue;
                    if (Player != null && c.DistanceTo(Player.Destination) < 2) continue;
                    options.Add(c);
                }
                if (options.Count == 0) continue;
                var path = Pathfinder.FindPath(g.Actor.Cell, options[UnityEngine.Random.Range(0, options.Count)], Map.IsWalkable);
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

            if (HoverGroup != null)
            {
                targetGroup = HoverGroup;
                approachTries = 0;
                ApproachGroup();
                return;
            }

            targetGroup = null;
            var cell = HoverCell.Value;
            if (!Map.IsWalkable(cell)) return;
            bool isExit = Map.Exits.Contains(cell);
            WalkPlayer(cell, isExit ? () => StartCoroutine(UseExit(cell)) : (Action)null);
        }

        public void OnRightClick()
        {
            if (Mode == GameMode.Fight) Fight.CancelSpell();
        }

        public void OnKey(KeyCode key)
        {
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

        bool WalkPlayer(Cell target, Action onArrive)
        {
            var path = Pathfinder.FindPath(Player.PlanningCell, target, Map.IsWalkable);
            if (path == null) return false;
            Player.Walk(path, onArrive);
            return true;
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

            List<Cell> best = null;
            foreach (var n in gc.Neighbors())
            {
                if (!Map.IsWalkable(n)) continue;
                var path = Pathfinder.FindPath(Player.PlanningCell, n, Map.IsWalkable);
                if (path != null && (best == null || path.Count < best.Count)) best = path;
            }
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
            int last = Map.Width - 1;
            int mx = Map.MapX, my = Map.MapY;
            Cell entry;
            if (exit.X == 0) { mx--; entry = new Cell(last, exit.Y); }
            else if (exit.X == last) { mx++; entry = new Cell(0, exit.Y); }
            else if (exit.Y == 0) { my--; entry = new Cell(exit.X, last); }
            else { my++; entry = new Cell(exit.X, 0); }

            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                FadeAlpha = t;
                yield return null;
            }
            FadeAlpha = 1f;
            LoadMap(mx, my, entry);
            // Sai um passo da borda para não ficar em cima da saída.
            var step = Pathfinder.FindPath(entry, Map.Center, Map.IsWalkable);
            if (step != null && step.Count > 0) Player.Walk(new List<Cell> { step[0] });
            AddLog($"Você chegou em {Map.Name} [{Map.MapX},{Map.MapY}].", Art.Hex("#9ad0ff"));
            Save();
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
            {
                FadeAlpha = 1f - t;
                yield return null;
            }
            FadeAlpha = 0f;
            changingMap = false;
        }

        // ================================================================== luta

        void StartFight(MonsterGroup group)
        {
            if (Mode != GameMode.Exploration) return;
            Mode = GameMode.Fight;
            targetGroup = null;
            fightGroup = group;
            Player.Stop();
            group.Actor.Stop();
            foreach (var g in Groups)
                if (g != group) g.Actor.gameObject.SetActive(false);
            Fight = gameObject.AddComponent<FightDirector>();
            Fight.Begin(this, group);
        }

        public void OnFightFinished(FightResult result)
        {
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
            if (victory)
            {
                respawnAt = Time.time + 20f;
            }
            else
            {
                Profile.Hp = Mathf.Max(1, Profile.MaxHp / 2);
                LoadMap(0, 0, null);
                AddLog("Você desmaiou e acordou nos Campos de Aldaria.", Art.Hex("#ff8a80"));
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
                if (!Map.IsWalkable(c)) continue;
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

        public void AddPopup(Vector3 world, string text, Color color, bool big = false)
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
            Cam.transform.position = new Vector3(0f, -4f, -10f);
        }

        void FitCamera()
        {
            if (Cam == null || Map == null) return;
            float halfW = (Map.Width + Map.Height) * 0.25f + 0.4f;
            float halfH = (Map.Width + Map.Height) * 0.125f + 1.3f;
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

            var center = Iso.ToWorld(Map.Center) + new Vector3(0f, -0.45f, 0f);
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
