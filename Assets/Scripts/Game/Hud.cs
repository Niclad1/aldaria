using System.Collections.Generic;
using System.Text;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>
    /// Toda a interface, feita com IMGUI (não precisa de Canvas nem prefabs). Também é por aqui
    /// que chegam cliques, teclas e rolagem do mouse — funciona com o Input Manager antigo e com o Input System novo.
    /// Os painéis de inventário, missões, conversa e loja ficam em HudPanels.cs.
    /// </summary>
    public sealed partial class Hud : MonoBehaviour
    {
        const float VH = 720f;

        GameController game;
        float scale = 1f, vw = 1280f;
        Vector2 mouse;
        readonly List<Rect> blockers = new List<Rect>();
        string tooltip;

        bool stylesReady;
        GUIStyle panel, dark, button, bigButton, smallButton, slot, slotOn, text, textLight, small, smallLight, smallCenter, header, headerLight, title, subtitle, field, bigNumber, popup, popupBig, keyLabel, marker;

        string nameInput = "";
        int classIndex;
        bool creating;

        static readonly Color Brown = new Color(0.25f, 0.15f, 0.07f);
        static readonly Color Cream = new Color(0.97f, 0.93f, 0.84f);
        static readonly Color GoldText = new Color(1f, 0.83f, 0.35f);

        void Awake() => game = GetComponent<GameController>();

#if ENABLE_LEGACY_INPUT_MANAGER
        void Update() => game.ScreenMouse = Input.mousePosition;
#endif

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.identity;
            var e = Event.current;
            var raw = e.mousePosition;
#if !ENABLE_LEGACY_INPUT_MANAGER
            game.ScreenMouse = new Vector2(raw.x, Screen.height - raw.y);
#endif
            scale = Screen.height / VH;
            vw = Screen.width / scale;
            mouse = raw / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            EnsureStyles();
            blockers.Clear();
            tooltip = null;

            switch (game.Mode)
            {
                case GameMode.Title: DrawTitle(); break;
                case GameMode.Exploration: DrawExploration(); break;
                case GameMode.Fight: DrawFight(); break;
            }
            if (game.LastResult != null) DrawResult(game.LastResult);
            DrawPopups();
            if (tooltip != null) DrawTooltip(tooltip);

            if (game.FadeAlpha > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, game.FadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, vw, VH), Art.White);
                GUI.color = Color.white;
            }

            HandleInput(e);
        }

        void HandleInput(Event e)
        {
            bool overUi = false;
            foreach (var r in blockers)
                if (r.Contains(mouse)) overUi = true;
            game.PointerOverUi = overUi;
            if (game.Mode == GameMode.Title) return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (overUi || game.LastResult != null) break;
                    if (e.button == 0) game.OnWorldClick();
                    else if (e.button == 1) game.OnRightClick();
                    e.Use();
                    break;
                case EventType.KeyDown:
                    if (e.keyCode != KeyCode.None && GUIUtility.keyboardControl == 0) game.OnKey(e.keyCode);
                    break;
                case EventType.ScrollWheel:
                    if (overUi) break;
                    game.Zoom(e.delta.y);
                    e.Use();
                    break;
            }
        }

        // ================================================================== telas

        void DrawTitle()
        {
            GUI.color = new Color(0.04f, 0.07f, 0.05f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, vw, VH), Art.White);
            GUI.color = Color.white;

            var saved = game.SavedProfile;
            bool showCreate = saved == null || creating;
            float w = showCreate ? 980f : 640f, h = showCreate ? 520f : 260f;
            var r = new Rect((vw - w) / 2f, (VH - h) / 2f + 44f, w, h);

            ShadowLabel(new Rect(0, r.y - 124, vw, 80), "Aldaria", title);
            ShadowLabel(new Rect(0, r.y - 54, vw, 30), "Um MMORPG tático por turnos", subtitle);
            Panel(r, panel);

            if (!showCreate)
            {
                var cls = Catalog.ClassById(saved.ClassId);
                GUI.Box(new Rect(r.x + 28, r.y + 28, 140, 200), GUIContent.none, slot);
                DrawFull(new Rect(r.x + 34, r.y + 32, 128, 192), Art.Character(saved.ClassId));
                GUI.Label(new Rect(r.x + 190, r.y + 30, 420, 36), saved.Name, header);
                GUI.Label(new Rect(r.x + 190, r.y + 68, 420, 24), $"{cls.Name} • Nível {saved.Level} • {saved.Gold} de ouro", text);
                GUI.Label(new Rect(r.x + 190, r.y + 94, 420, 24), $"Último mapa: [{saved.MapX},{saved.MapY}] • {saved.Victories} vitórias", text);
                if (GUI.Button(new Rect(r.x + 190, r.y + 132, 260, 54), "Continuar", bigButton)) game.StartGame(saved);
                if (GUI.Button(new Rect(r.x + 190, r.y + 196, 260, 36), "Criar novo personagem", smallButton))
                {
                    creating = true;
                    nameInput = "";
                }
            }
            else
            {
                GUI.Label(new Rect(r.x + 30, r.y + 24, 300, 24), "Nome do personagem", text);
                nameInput = Sanitize(GUI.TextField(new Rect(r.x + 30, r.y + 50, 320, 36), nameInput, 16, field));
                GUI.Label(new Rect(r.x + 380, r.y + 24, 300, 24), "Escolha sua classe", text);

                int n = Catalog.Classes.Count;
                float cw = (w - 60 - (n - 1) * 10) / n;
                for (int i = 0; i < n; i++)
                {
                    var cls = Catalog.Classes[i];
                    var card = new Rect(r.x + 30 + i * (cw + 10), r.y + 100, cw, 220);
                    if (GUI.Button(card, GUIContent.none, i == classIndex ? slotOn : slot)) classIndex = i;
                    DrawFull(new Rect(card.x + 10, card.y + 8, card.width - 20, 160), Art.Character(cls.Id));
                    ShadowLabel(new Rect(card.x, card.y + 168, card.width, 26), cls.Name, new GUIStyle(headerLight) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
                    GUI.Label(new Rect(card.x, card.y + 192, card.width, 20), cls.Role, smallCenter);
                }

                var sel = Catalog.Classes[classIndex];
                var sb = new StringBuilder();
                sb.Append("<b>").Append(sel.Name).Append(":</b> ").Append(sel.Description).Append("\n<b>Feitiços:</b> ");
                for (int i = 0; i < sel.Spells.Count; i++) sb.Append(i > 0 ? ", " : "").Append(sel.Spells[i].Name);
                sb.Append($"\n<b>PV:</b> {sel.BaseHp}  •  <b>PA:</b> {sel.Ap}  •  <b>PM:</b> {sel.Mp}  •  <b>Iniciativa:</b> {sel.Initiative}");
                GUI.Label(new Rect(r.x + 30, r.y + 334, w - 340, 110), sb.ToString(), text);
                for (int i = 0; i < sel.Spells.Count; i++)
                {
                    var ir = new Rect(r.x + w - 290 + i * 66, r.y + 340, 58, 58);
                    GUI.DrawTexture(ir, Art.SpellIcon(sel.Spells[i]));
                    if (ir.Contains(mouse)) tooltip = SpellTooltip(sel.Spells[i], null);
                }

                if (GUI.Button(new Rect(r.x + w - 290, r.y + h - 78, 260, 54), "Começar aventura", bigButton))
                {
                    var name = nameInput.Trim();
                    creating = false;
                    game.StartGame(PlayerProfile.Create(name.Length > 0 ? name : "Aventureiro", sel.Id));
                }
                if (saved != null && GUI.Button(new Rect(r.x + 30, r.y + h - 70, 150, 38), "Voltar", smallButton)) creating = false;
            }

            ShadowLabel(new Rect(0, VH - 34, vw, 24), "Clique para andar  •  I inventário  •  J missões  •  1-4 feitiços  •  Espaço passa o turno  •  Rolagem = zoom", smallCenter);
        }

        void DrawExploration()
        {
            var p = game.Profile;
            DrawNpcMarkers();
            DrawPlayerPanel(p.Hp, p.MaxHp);
            DrawMapPanel();
            DrawQuestTracker();
            DrawChat(new Rect(12, VH - 176, 400, 164));

            float bx = vw - 12;
            if (GUI.Button(Block(new Rect(bx -= 96, VH - 56, 96, 44)), "Menu", smallButton)) game.BackToTitle();
            if (GUI.Button(Block(new Rect(bx -= 138, VH - 56, 132, 44)), "Missões (J)", smallButton)) game.TogglePanel(UiPanel.Quests);
            if (GUI.Button(Block(new Rect(bx -= 150, VH - 56, 144, 44)), "Inventário (I)", smallButton)) game.TogglePanel(UiPanel.Inventory);

            switch (game.OpenPanel)
            {
                case UiPanel.Inventory: DrawInventory(); break;
                case UiPanel.Quests: DrawQuestLog(); break;
                case UiPanel.Dialogue: DrawDialogue(); break;
                case UiPanel.Shop: DrawShop(); break;
            }

            if (game.HoverGroup != null && game.OpenPanel == UiPanel.None)
            {
                var g = game.HoverGroup;
                var sb = new StringBuilder(g.IsBoss ? "<b><color=#ff9a6a>Chefe!</color></b>  " : "");
                sb.Append($"<b>Grupo de monstros</b>  (nível total {g.TotalLevel})");
                foreach (var (def, level) in g.Members) sb.Append($"\n• {def.Name} — nível {level}");
                sb.Append("\n<color=#ffd166>Clique para lutar!</color>");
                tooltip = sb.ToString();
            }
            else if (game.HoverNpc != null && game.OpenPanel == UiPanel.None)
            {
                var n = game.HoverNpc.Def;
                tooltip = $"<b>{n.Name}</b>\n<color=#c9b48a>{n.Title}</color>\n<color=#ffd166>Clique para conversar</color>";
            }
        }

        void DrawFight()
        {
            var fd = game.Fight;
            if (fd == null) return;
            var fight = fd.Fight;

            DrawOverheadBars(fd);
            DrawPlayerPanel(fd.Player.Hp, fd.Player.MaxHp);

            string status;
            if (fight.Phase == FightPhase.Placement) status = "Posicionamento — escolha uma célula azul";
            else if (fight.Phase == FightPhase.Ended) status = "Fim da luta";
            else if (fight.Current == fd.Player) status = $"Rodada {fight.Round} — <color=#ffd166>Sua vez</color>";
            else status = $"Rodada {fight.Round} — Vez de {fight.Current.Name}";
            var sr = Block(new Rect(vw / 2f - 210, 12, 420, 44));
            GUI.Box(sr, GUIContent.none, dark);
            GUI.Label(sr, status, new GUIStyle(textLight) { alignment = TextAnchor.MiddleCenter, fontSize = 17 });

            DrawTimeline(fd);
            DrawChat(new Rect(12, VH - 176, 330, 164));
            DrawActionBar(fd);

            if (!game.PointerOverUi && game.LastResult == null)
            {
                foreach (var kv in fd.Actors)
                {
                    var f = kv.Key;
                    if (!f.IsAlive) continue;
                    if (!kv.Value.HitTest(game.MouseWorld) && !(game.HoverCell.HasValue && game.HoverCell.Value == f.Cell)) continue;
                    var sb = new StringBuilder($"<b>{f.Name}</b>  (nível {f.Level})\nPV {f.Hp}/{f.MaxHp}");
                    bool active = fight.Current == f;
                    sb.Append($"\nPA {(active ? f.Ap : f.BaseAp)}  •  PM {(active ? f.Mp : Mathf.Max(0, f.BaseMp + f.BuffTotal(BuffStat.Mp)))}");
                    foreach (var b in f.Buffs) sb.Append($"\n<color=#c5b3ff>{b.Source}: {BuffText(b)} ({b.TurnsLeft}t)</color>");
                    tooltip = sb.ToString();
                }
            }
        }

        static string BuffText(Buff b)
        {
            switch (b.Stat)
            {
                case BuffStat.Poison: return $"veneno {b.Value}/turno";
                case BuffStat.Damage: return $"{(b.Value > 0 ? "+" : "")}{b.Value} dano";
                case BuffStat.Ap: return $"{(b.Value > 0 ? "+" : "")}{b.Value} PA";
                default: return $"{(b.Value > 0 ? "+" : "")}{b.Value} PM";
            }
        }

        void DrawResult(FightResult r)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, vw, VH), Art.White);
            GUI.color = Color.white;

            float h = r.Victory && r.Drops.Count > 0 ? 330 : 260;
            var rect = Panel(new Rect(vw / 2f - 220, VH / 2f - h / 2, 440, h), panel);
            var t = new GUIStyle(title) { fontSize = 44, normal = { textColor = r.Victory ? GoldText : new Color(1f, 0.45f, 0.4f) } };
            ShadowLabel(new Rect(rect.x, rect.y + 20, rect.width, 56), r.Victory ? "Vitória!" : "Derrota", t);
            string body = r.Victory
                ? $"+{r.Xp} de experiência    +{r.Gold} de ouro" + (r.LevelsGained > 0 ? $"\n<color=#b8860b><b>Subiu para o nível {r.NewLevel}!</b></color>" : "")
                : "Você desmaiou...\nVai acordar na Vila de Aldaria.";
            GUI.Label(new Rect(rect.x + 20, rect.y + 84, rect.width - 40, 60), body, new GUIStyle(text) { alignment = TextAnchor.UpperCenter, fontSize = 17 });
            if (r.Victory && r.Drops.Count > 0)
            {
                GUI.Label(new Rect(rect.x + 20, rect.y + 142, rect.width - 40, 22), "Itens encontrados", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
                float x = rect.x + rect.width / 2 - r.Drops.Count * 34;
                foreach (var d in r.Drops)
                {
                    var it = Catalog.ItemById(d.Id);
                    var sr = new Rect(x, rect.y + 168, 60, 60);
                    ItemSlotBox(sr, it, d.Count, false);
                    if (sr.Contains(mouse)) tooltip = ItemTooltip(it, "");
                    x += 68;
                }
            }
            if (GUI.Button(new Rect(rect.x + 120, rect.y + h - 70, 200, 50), "Continuar", bigButton)) game.CloseFightResult();
        }

        // ================================================================== blocos

        void DrawPlayerPanel(int hp, int maxHp)
        {
            var p = game.Profile;
            var r = Panel(new Rect(12, 12, 320, 118), panel);
            GUI.Box(new Rect(r.x + 14, r.y + 14, 90, 90), GUIContent.none, slot);
            Art.DrawPortrait(new Rect(r.x + 19, r.y + 18, 80, 80), Art.Character(p.ClassId));
            GUI.Label(new Rect(r.x + 114, r.y + 12, 196, 28), p.Name, header);
            GUI.Label(new Rect(r.x + 114, r.y + 38, 196, 20), $"{p.Class.Name} • Nv {p.Level} • {p.Gold} ouro", small);
            Bar(new Rect(r.x + 114, r.y + 62, 190, 20), (float)hp / Mathf.Max(1, maxHp), new Color(0.86f, 0.22f, 0.22f), $"{hp} / {maxHp} PV");
            int next = Progression.XpToNext(p.Level);
            Bar(new Rect(r.x + 114, r.y + 88, 190, 14), (float)p.Xp / next, new Color(0.95f, 0.72f, 0.2f), $"XP {p.Xp} / {next}");
        }

        void DrawMapPanel()
        {
            var map = game.Map;
            var r = Block(new Rect(vw / 2f - 190, 12, 380, 44));
            GUI.Box(r, GUIContent.none, dark);
            GUI.Label(r, $"{map.Name}  <color=#ffd166>[{map.MapX},{map.MapY}]</color>  <color=#c9b48a>{Catalog.RegionName(map.Region)}</color>",
                new GUIStyle(textLight) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
        }

        void DrawNpcMarkers()
        {
            foreach (var n in game.Npcs)
            {
                var m = game.NpcMarker(n.Def);
                var g = WorldToGui(n.Actor.HeadPosition);
                if (m != null)
                {
                    var col = m == "?" ? GoldText : m == "!" ? new Color(1f, 0.9f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);
                    ShadowLabel(new Rect(g.x - 30, g.y - 50, 60, 44), m, new GUIStyle(marker) { normal = { textColor = col } });
                }
            }
        }

        void DrawChat(Rect r)
        {
            Panel(r, dark);
            var inner = new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20);
            float y = inner.yMax;
            GUI.BeginGroup(inner);
            for (int i = game.Log.Count - 1; i >= 0 && y > inner.y; i--)
            {
                var line = game.Log[i];
                var content = new GUIContent($"<color=#{ColorUtility.ToHtmlStringRGB(line.Color)}>{line.Text}</color>");
                float h = smallLight.CalcHeight(content, inner.width);
                y -= h;
                GUI.Label(new Rect(0, y - inner.y, inner.width, h), content, smallLight);
            }
            GUI.EndGroup();
        }

        void DrawTimeline(FightDirector fd)
        {
            var fight = fd.Fight;
            var list = fight.Phase == FightPhase.Placement ? (IReadOnlyList<Fighter>)fight.Fighters : fight.TurnOrder;
            var r = Panel(new Rect(vw - 244, 12, 232, 42 + list.Count * 52), dark);
            GUI.Label(new Rect(r.x + 14, r.y + 10, 200, 22), "Ordem dos turnos", smallLight);
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                var row = new Rect(r.x + 8, r.y + 34 + i * 52, r.width - 16, 48);
                if (fight.Current == f) GUI.Box(row, GUIContent.none, slotOn);
                var baseCol = f.IsAlive ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                GUI.color = baseCol;
                var stripe = f.Team == Team.Players ? new Color(0.3f, 0.55f, 1f, baseCol.a) : new Color(1f, 0.35f, 0.3f, baseCol.a);
                GUI.color = stripe;
                GUI.DrawTexture(new Rect(row.x + 4, row.y + 8, 4, 32), Art.White);
                GUI.color = baseCol;
                Art.DrawPortrait(new Rect(row.x + 12, row.y + 4, 40, 40), Art.Character(f.Visual));
                GUI.Label(new Rect(row.x + 58, row.y + 4, 150, 20), $"{f.Name} <color=#c9b48a>nv {f.Level}</color>", smallLight);
                Bar(new Rect(row.x + 58, row.y + 28, 146, 12), (float)f.Hp / f.MaxHp, stripe, "");
                GUI.color = Color.white;
            }
        }

        void DrawActionBar(FightDirector fd)
        {
            var fight = fd.Fight;
            var player = fd.Player;
            float w = 580f;
            var r = Panel(new Rect(Mathf.Max(vw / 2f - w / 2f, 352f), VH - 124, w, 112), panel);
            bool placing = fight.Phase == FightPhase.Placement;
            bool myTurn = fd.IsPlayerTurn;

            int ap = fight.Current == player ? player.Ap : player.BaseAp;
            int mp = fight.Current == player ? player.Mp : player.BaseMp;
            Orb(new Rect(r.x + 16, r.y + 14, 74, 40), ap.ToString(), "PA", new Color(0.4f, 0.65f, 1f));
            Orb(new Rect(r.x + 16, r.y + 58, 74, 40), mp.ToString(), "PM", new Color(0.5f, 0.88f, 0.4f));

            for (int i = 0; i < player.Spells.Count && i < 4; i++)
            {
                var s = player.Spells[i];
                var slotRect = new Rect(r.x + 102 + i * 80, r.y + 20, 72, 72);
                bool selected = fd.Selected == s;
                if (GUI.Button(slotRect, GUIContent.none, selected ? slotOn : slot)) fd.SelectSpell(i);
                GUI.DrawTexture(new Rect(slotRect.x + 7, slotRect.y + 7, 58, 58), Art.SpellIcon(s));

                string problem = placing ? "Aguarde o início da luta" : fd.SpellProblem(s);
                if (problem != null || !myTurn)
                {
                    GUI.color = new Color(0f, 0f, 0f, 0.5f);
                    GUI.DrawTexture(new Rect(slotRect.x + 7, slotRect.y + 7, 58, 58), Art.White);
                    GUI.color = Color.white;
                }
                int cd = player.CooldownOf(s);
                if (cd > 0) ShadowLabel(slotRect, cd.ToString(), bigNumber);
                ShadowLabel(new Rect(slotRect.x + 6, slotRect.y + 3, 20, 18), (i + 1).ToString(), keyLabel);
                ShadowLabel(new Rect(slotRect.x + 42, slotRect.y + 50, 26, 18), s.ApCost.ToString(), new GUIStyle(keyLabel) { alignment = TextAnchor.LowerRight, normal = { textColor = new Color(0.6f, 0.8f, 1f) } });
                if (slotRect.Contains(mouse)) tooltip = SpellTooltip(s, placing ? null : problem);
            }

            var br = new Rect(r.x + w - 150, r.y + 18, 134, 48);
            if (placing)
            {
                if (GUI.Button(br, "Pronto!", bigButton)) fd.Ready();
            }
            else
            {
                GUI.enabled = myTurn;
                if (GUI.Button(br, myTurn ? "Passar turno" : "Aguarde...", button)) fd.EndPlayerTurn();
                GUI.enabled = true;
                if (fight.Current == player)
                    Bar(new Rect(br.x, r.y + 76, br.width, 16), fd.TurnTimeLeft / FightDirector.TurnTime, new Color(0.95f, 0.72f, 0.2f), $"{Mathf.CeilToInt(Mathf.Max(0f, fd.TurnTimeLeft))}s");
            }
        }

        string SpellTooltip(SpellDef s, string problem)
        {
            var sb = new StringBuilder($"<b>{s.Name}</b>  <color=#9ad0ff>{s.ApCost} PA</color>  •  alcance {s.RangeText}");
            if (s.Effect == SpellEffect.Damage) sb.Append($"\nDano {s.Min}-{s.Max} ({ElementName(s.Element)})");
            if (s.Effect == SpellEffect.Heal) sb.Append($"\nCura {s.Min}-{s.Max}");
            if (s.Area > 0) sb.Append(s.Target == SpellTarget.Self ? $"\nÁrea ao seu redor ({s.Area})" : "\nÁrea em cruz");
            if (s.InLineOnly) sb.Append("\nSó em linha reta");
            if (!s.NeedsSight && s.Target != SpellTarget.Self) sb.Append("\nNão precisa de linha de visão");
            if (s.Cooldown > 0) sb.Append($"\nRecarga: {s.Cooldown} turno(s)");
            sb.Append($"\n<i>{s.Description}</i>");
            if (problem != null) sb.Append($"\n<color=#ff8a80>{problem}</color>");
            return sb.ToString();
        }

        void DrawOverheadBars(FightDirector fd)
        {
            foreach (var kv in fd.Actors)
            {
                var f = kv.Key;
                if (!f.IsAlive) continue;
                var g = WorldToGui(kv.Value.HeadPosition);
                var color = f.Team == Team.Players ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.35f, 0.3f);
                Bar(new Rect(g.x - 26, g.y - 10, 52, 8), (float)f.Hp / f.MaxHp, color, "");
                if (f.Buffs.Exists(b => b.Stat == BuffStat.Poison))
                    ShadowLabel(new Rect(g.x + 28, g.y - 18, 20, 20), "☠", new GUIStyle(keyLabel) { normal = { textColor = new Color(0.6f, 1f, 0.35f) } });
            }
        }

        void DrawPopups()
        {
            foreach (var p in game.Popups)
            {
                var g = WorldToGui(p.World + Vector3.up * (p.Age * 0.45f));
                float a = 1f - Mathf.Clamp01((p.Age - 0.8f) / 0.5f);
                var style = p.Big ? popupBig : popup;
                var rect = new Rect(g.x - 120, g.y - 40, 240, 34);
                style.normal.textColor = new Color(0f, 0f, 0f, 0.75f * a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), p.Text, style);
                style.normal.textColor = new Color(p.Color.r, p.Color.g, p.Color.b, a);
                GUI.Label(rect, p.Text, style);
            }
        }

        void DrawTooltip(string content)
        {
            var gc = new GUIContent(content);
            float w = 300f;
            float h = smallLight.CalcHeight(gc, w - 24) + 20;
            float x = Mathf.Min(mouse.x + 18, vw - w - 6);
            float y = Mathf.Min(mouse.y + 18, VH - h - 6);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, dark);
            GUI.Label(new Rect(x + 12, y + 10, w - 24, h - 20), gc, smallLight);
        }

        // ================================================================== peças

        Rect Panel(Rect r, GUIStyle style)
        {
            GUI.Box(r, GUIContent.none, style);
            blockers.Add(r);
            return r;
        }

        Rect Block(Rect r)
        {
            blockers.Add(r);
            return r;
        }

        static void DrawFull(Rect r, Sprite s)
        {
            if (s != null) GUI.DrawTexture(r, s.texture, ScaleMode.ScaleToFit);
        }

        void Bar(Rect r, float t, Color fill, string label)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.12f, 0.08f, 0.06f, 0.9f * prev.a);
            GUI.DrawTexture(r, Art.White);
            GUI.color = new Color(fill.r, fill.g, fill.b, prev.a);
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(t), r.height - 2), Art.White);
            GUI.color = new Color(1f, 1f, 1f, 0.25f * prev.a);
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(t), Mathf.Max(1f, (r.height - 2) * 0.4f)), Art.White);
            GUI.color = prev;
            if (!string.IsNullOrEmpty(label)) ShadowLabel(r, label, new GUIStyle(smallCenter) { fontSize = r.height >= 18 ? 13 : 11, normal = { textColor = Color.white } });
        }

        void Orb(Rect r, string value, string label, Color color)
        {
            GUI.Box(r, GUIContent.none, slot);
            ShadowLabel(new Rect(r.x + 6, r.y, 38, r.height), value, new GUIStyle(bigNumber) { fontSize = 26, normal = { textColor = color } });
            GUI.Label(new Rect(r.x + 42, r.y, 30, r.height), label, new GUIStyle(smallLight) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold });
        }

        void ShadowLabel(Rect r, string content, GUIStyle style)
        {
            var c = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.6f * c.a);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), StripColor(content), style);
            style.normal.textColor = c;
            GUI.Label(r, content, style);
        }

        static string StripColor(string s)
        {
            if (s.IndexOf('<') < 0) return s;
            var sb = new StringBuilder();
            bool tag = false;
            foreach (var ch in s)
            {
                if (ch == '<') tag = true;
                else if (ch == '>') tag = false;
                else if (!tag) sb.Append(ch);
            }
            return sb.ToString();
        }

        Vector2 WorldToGui(Vector3 world)
        {
            var s = game.Cam.WorldToScreenPoint(world);
            return new Vector2(s.x / scale, (Screen.height - s.y) / scale);
        }

        static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in s)
                if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_') sb.Append(ch);
            return sb.ToString();
        }

        static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Earth: return "Terra";
                case Element.Fire: return "Fogo";
                case Element.Water: return "Água";
                case Element.Air: return "Ar";
                default: return "Neutro";
            }
        }

        static GUIStyle Box(string artName, System.Func<Texture2D> fallback, RectOffset fallbackBorder)
        {
            var tex = Art.Get(artName)?.texture;
            return new GUIStyle
            {
                normal = { background = tex != null ? tex : fallback() },
                border = tex != null ? Art.UiBorder(artName) : fallbackBorder,
            };
        }

        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            var font = Art.UiFont(false);
            var bold = Art.UiFont(true);
            var b9 = new RectOffset(9, 9, 9, 9);

            panel = Box("ui_panel", () => Art.LegacyPanel(), b9);
            dark = Box("ui_dark", () => Art.LegacyDark(), b9);
            slot = Box("ui_slot", () => Art.LegacySlot(false), b9);
            slot.hover.background = Art.Get("ui_slot_hover")?.texture ?? Art.LegacySlot(true);
            slot.active.background = Art.Get("ui_slot_on")?.texture ?? Art.LegacySlot(true);
            slotOn = Box("ui_slot_on", () => Art.LegacySlot(true), b9);
            slotOn.hover.background = slotOn.normal.background;
            slotOn.active.background = slotOn.normal.background;

            button = Box("ui_button", () => Art.LegacyButton(0), b9);
            button.hover.background = Art.Get("ui_button_hover")?.texture ?? Art.LegacyButton(1);
            button.active.background = Art.Get("ui_button_down")?.texture ?? Art.LegacyButton(2);
            button.focused.background = button.normal.background;
            button.normal.textColor = button.hover.textColor = button.focused.textColor = Color.white;
            button.active.textColor = new Color(1f, 0.95f, 0.85f);
            button.alignment = TextAnchor.MiddleCenter;
            button.font = bold;
            button.fontSize = 16;
            button.padding = new RectOffset(8, 8, 4, 6);
            bigButton = new GUIStyle(button) { fontSize = 21 };
            smallButton = new GUIStyle(button) { fontSize = 14 };

            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 15, wordWrap = true, richText = true, normal = { textColor = Brown } };
            textLight = new GUIStyle(text) { normal = { textColor = Cream } };
            small = new GUIStyle(text) { fontSize = 13 };
            smallLight = new GUIStyle(small) { normal = { textColor = Cream } };
            smallCenter = new GUIStyle(smallLight) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            header = new GUIStyle(text) { font = bold, fontSize = 21, wordWrap = false };
            headerLight = new GUIStyle(header) { normal = { textColor = Cream } };
            title = new GUIStyle(text) { font = bold, fontSize = 76, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = GoldText } };
            subtitle = new GUIStyle(text) { fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = Cream } };
            field = new GUIStyle(GUI.skin.textField)
            {
                font = font,
                fontSize = 18,
                padding = new RectOffset(12, 12, 7, 7),
                normal = { background = Art.LegacyField(false), textColor = Brown },
                focused = { background = Art.LegacyField(true), textColor = Brown },
                hover = { background = Art.LegacyField(false), textColor = Brown },
                border = new RectOffset(8, 8, 8, 8),
            };
            bigNumber = new GUIStyle(text) { font = bold, fontSize = 30, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = Color.white } };
            keyLabel = new GUIStyle(text) { font = bold, fontSize = 13, wordWrap = false, normal = { textColor = Cream } };
            popup = new GUIStyle(text) { font = bold, fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            popupBig = new GUIStyle(popup) { fontSize = 26 };
            marker = new GUIStyle(text) { font = bold, fontSize = 40, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        }
    }
}
