using System.Collections.Generic;
using System.Text;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>
    /// Toda a interface, feita com IMGUI (não precisa de Canvas nem prefabs). Também é por aqui
    /// que chegam cliques, teclas e rolagem do mouse — funciona com o Input Manager antigo e com o Input System novo.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        const float VH = 720f;

        GameController game;
        float scale = 1f, vw = 1280f;
        Vector2 mouse;
        readonly List<Rect> blockers = new List<Rect>();
        string tooltip;

        bool stylesReady;
        GUIStyle panel, dark, button, bigButton, smallButton, slot, slotOn, text, textLight, textCenter, small, smallLight, smallCenter, header, title, subtitle, field, bigNumber, popup, popupBig, keyLabel;

        string nameInput = "";
        int classIndex;
        bool creating;

        static readonly Color Brown = new Color(0.23f, 0.15f, 0.09f);
        static readonly Color Cream = new Color(0.96f, 0.93f, 0.85f);

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
                    if (e.keyCode != KeyCode.None) game.OnKey(e.keyCode);
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
            GUI.color = new Color(0.04f, 0.07f, 0.05f, 0.5f);
            GUI.DrawTexture(new Rect(0, 0, vw, VH), Art.White);
            GUI.color = Color.white;

            var saved = game.SavedProfile;
            bool showCreate = saved == null || creating;
            float w = 660f, h = showCreate ? 520f : 250f;
            var r = new Rect((vw - w) / 2f, (VH - h) / 2f + 36f, w, h);

            ShadowLabel(new Rect(0, r.y - 112, vw, 70), "ALDARIA", title);
            ShadowLabel(new Rect(0, r.y - 50, vw, 30), "Um MMORPG tático por turnos", subtitle);
            Panel(r, panel);

            if (!showCreate)
            {
                var cls = Catalog.ClassById(saved.ClassId);
                GUI.Box(new Rect(r.x + 24, r.y + 24, 130, 150), GUIContent.none, slot);
                GUI.DrawTexture(new Rect(r.x + 34, r.y + 30, 110, 138), Art.Character(saved.ClassId).texture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(r.x + 176, r.y + 26, 460, 34), saved.Name, header);
                GUI.Label(new Rect(r.x + 176, r.y + 62, 460, 24), $"{cls.Name} • Nível {saved.Level} • {saved.Gold} de ouro", text);
                GUI.Label(new Rect(r.x + 176, r.y + 86, 460, 24), $"Último mapa: [{saved.MapX},{saved.MapY}] • {saved.Victories} vitórias", text);
                if (GUI.Button(new Rect(r.x + 176, r.y + 124, 250, 50), "Continuar", bigButton)) game.StartGame(saved);
                if (GUI.Button(new Rect(r.x + 176, r.y + 190, 250, 34), "Criar novo personagem", smallButton))
                {
                    creating = true;
                    nameInput = "";
                }
            }
            else
            {
                GUI.Label(new Rect(r.x + 24, r.y + 18, 300, 24), "Nome do personagem", text);
                nameInput = Sanitize(GUI.TextField(new Rect(r.x + 24, r.y + 44, 300, 32), nameInput, 16, field));
                GUI.Label(new Rect(r.x + 24, r.y + 88, 300, 24), "Escolha sua classe", text);

                for (int i = 0; i < Catalog.Classes.Count; i++)
                {
                    var cls = Catalog.Classes[i];
                    var card = new Rect(r.x + 24 + i * 208, r.y + 116, 196, 196);
                    if (GUI.Button(card, GUIContent.none, i == classIndex ? slotOn : slot)) classIndex = i;
                    GUI.DrawTexture(new Rect(card.x + 48, card.y + 10, 100, 122), Art.Character(cls.Id).texture, ScaleMode.ScaleToFit);
                    ShadowLabel(new Rect(card.x, card.y + 138, card.width, 26), cls.Name, new GUIStyle(header) { alignment = TextAnchor.MiddleCenter, fontSize = 18, normal = { textColor = Cream } });
                    GUI.Label(new Rect(card.x, card.y + 164, card.width, 20), cls.Role, smallCenter);
                }

                var sel = Catalog.Classes[classIndex];
                var sb = new StringBuilder();
                sb.Append(sel.Description).Append("\n<b>Feitiços:</b> ");
                for (int i = 0; i < sel.Spells.Count; i++) sb.Append(i > 0 ? ", " : "").Append(sel.Spells[i].Name);
                sb.Append($"\n<b>PV:</b> {sel.BaseHp}  •  <b>PA:</b> {sel.Ap}  •  <b>PM:</b> {sel.Mp}");
                GUI.Label(new Rect(r.x + 24, r.y + 324, w - 48, 90), sb.ToString(), text);

                if (GUI.Button(new Rect(r.x + w - 274, r.y + h - 74, 250, 50), "Começar aventura", bigButton))
                {
                    var name = nameInput.Trim();
                    var profile = new PlayerProfile { Name = name.Length > 0 ? name : "Aventureiro", ClassId = sel.Id };
                    profile.Hp = profile.MaxHp;
                    creating = false;
                    game.StartGame(profile);
                }
                if (saved != null && GUI.Button(new Rect(r.x + 24, r.y + h - 66, 140, 34), "Voltar", smallButton)) creating = false;
            }

            GUI.Label(new Rect(0, VH - 34, vw, 24), "Clique para andar  •  1-4 escolhem feitiços  •  Espaço passa o turno  •  Rolagem do mouse dá zoom", smallCenter);
        }

        void DrawExploration()
        {
            var p = game.Profile;
            DrawPlayerPanel(p.Hp, p.MaxHp);
            DrawMapPanel();
            DrawChat(new Rect(12, VH - 172, 380, 160));

            if (GUI.Button(Block(new Rect(vw - 112, 12, 100, 34)), "Menu", smallButton)) game.BackToTitle();

            if (game.HoverGroup != null)
            {
                var sb = new StringBuilder($"<b>Grupo de monstros</b>  (nível total {game.HoverGroup.TotalLevel})");
                foreach (var (def, level) in game.HoverGroup.Members) sb.Append($"\n• {def.Name} — nível {level}");
                sb.Append("\n<color=#ffd166>Clique para lutar!</color>");
                tooltip = sb.ToString();
            }

            ShadowLabel(new Rect(400, VH - 34, vw - 800, 24), "Brilho dourado nas bordas = saída para outro mapa", smallCenter);
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
            var sr = Block(new Rect(vw / 2f - 200, 12, 400, 40));
            GUI.Box(sr, GUIContent.none, dark);
            GUI.Label(sr, status, new GUIStyle(textLight) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });

            DrawTimeline(fd);
            DrawChat(new Rect(12, VH - 172, 330, 160));
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
                    foreach (var b in f.Buffs) sb.Append($"\n<color=#c5b3ff>{b.Source}: {(b.Value > 0 ? "+" : "")}{b.Value} {(b.Stat == BuffStat.Damage ? "dano" : b.Stat == BuffStat.Ap ? "PA" : "PM")} ({b.TurnsLeft}t)</color>");
                    tooltip = sb.ToString();
                }
            }
        }

        void DrawResult(FightResult r)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(0, 0, vw, VH), Art.White);
            GUI.color = Color.white;

            var rect = Panel(new Rect(vw / 2f - 200, VH / 2f - 130, 400, 250), panel);
            var t = new GUIStyle(title) { fontSize = 38, normal = { textColor = r.Victory ? new Color(1f, 0.82f, 0.3f) : new Color(1f, 0.45f, 0.4f) } };
            ShadowLabel(new Rect(rect.x, rect.y + 16, rect.width, 50), r.Victory ? "Vitória!" : "Derrota", t);
            string body = r.Victory
                ? $"+{r.Xp} de experiência\n+{r.Gold} de ouro" + (r.LevelsGained > 0 ? $"\n<color=#b8860b><b>Subiu para o nível {r.NewLevel}!</b></color>" : "")
                : "Você desmaiou...\nVai acordar nos Campos de Aldaria.";
            GUI.Label(new Rect(rect.x + 20, rect.y + 76, rect.width - 40, 90), body, new GUIStyle(text) { alignment = TextAnchor.UpperCenter, fontSize = 17 });
            if (GUI.Button(new Rect(rect.x + 110, rect.y + 180, 180, 46), "Continuar", bigButton)) game.CloseFightResult();
        }

        // ================================================================== blocos

        void DrawPlayerPanel(int hp, int maxHp)
        {
            var p = game.Profile;
            var r = Panel(new Rect(12, 12, 300, 108), panel);
            GUI.Box(new Rect(r.x + 10, r.y + 10, 84, 88), GUIContent.none, slot);
            GUI.DrawTexture(new Rect(r.x + 14, r.y + 13, 76, 82), Art.Character(p.ClassId).texture, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(r.x + 104, r.y + 6, 190, 28), p.Name, header);
            GUI.Label(new Rect(r.x + 104, r.y + 32, 190, 20), $"{p.Class.Name} • Nv {p.Level} • {p.Gold} ouro", small);
            Bar(new Rect(r.x + 104, r.y + 56, 184, 20), (float)hp / Mathf.Max(1, maxHp), new Color(0.85f, 0.2f, 0.2f), $"{hp} / {maxHp} PV");
            int next = Progression.XpToNext(p.Level);
            Bar(new Rect(r.x + 104, r.y + 82, 184, 14), (float)p.Xp / next, new Color(0.95f, 0.72f, 0.2f), $"XP {p.Xp} / {next}");
        }

        void DrawMapPanel()
        {
            var map = game.Map;
            var r = Block(new Rect(vw / 2f - 170, 12, 340, 40));
            GUI.Box(r, GUIContent.none, dark);
            GUI.Label(r, $"{map.Name}  <color=#ffd166>[{map.MapX},{map.MapY}]</color>", new GUIStyle(textLight) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
        }

        void DrawChat(Rect r)
        {
            Panel(r, dark);
            var inner = new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16);
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
            var r = Panel(new Rect(vw - 232, 12, 220, 34 + list.Count * 48), dark);
            GUI.Label(new Rect(r.x + 12, r.y + 6, 200, 22), "Ordem dos turnos", smallLight);
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                var row = new Rect(r.x + 8, r.y + 30 + i * 48, r.width - 16, 44);
                if (fight.Current == f) GUI.Box(row, GUIContent.none, slotOn);
                GUI.color = f.IsAlive ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                var stripe = f.Team == Team.Players ? new Color(0.3f, 0.55f, 1f, GUI.color.a) : new Color(1f, 0.35f, 0.3f, GUI.color.a);
                var prev = GUI.color;
                GUI.color = stripe;
                GUI.DrawTexture(new Rect(row.x + 3, row.y + 6, 4, 32), Art.White);
                GUI.color = prev;
                GUI.DrawTexture(new Rect(row.x + 10, row.y + 3, 38, 38), Art.Character(f.Visual).texture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(row.x + 54, row.y + 2, 150, 20), $"{f.Name} <color=#c9b48a>nv {f.Level}</color>", smallLight);
                Bar(new Rect(row.x + 54, row.y + 24, 140, 12), (float)f.Hp / f.MaxHp, stripe, "");
                GUI.color = Color.white;
            }
        }

        void DrawActionBar(FightDirector fd)
        {
            var fight = fd.Fight;
            var player = fd.Player;
            float w = 560f;
            var r = Panel(new Rect(Mathf.Max(vw / 2f - w / 2f, 352f), VH - 116, w, 104), panel);
            bool placing = fight.Phase == FightPhase.Placement;
            bool myTurn = fd.IsPlayerTurn;

            int ap = fight.Current == player ? player.Ap : player.BaseAp;
            int mp = fight.Current == player ? player.Mp : player.BaseMp;
            Orb(new Rect(r.x + 12, r.y + 10, 70, 40), ap.ToString(), "PA", new Color(0.35f, 0.6f, 1f));
            Orb(new Rect(r.x + 12, r.y + 54, 70, 40), mp.ToString(), "PM", new Color(0.45f, 0.85f, 0.35f));

            for (int i = 0; i < player.Spells.Count && i < 4; i++)
            {
                var s = player.Spells[i];
                var slotRect = new Rect(r.x + 92 + i * 80, r.y + 16, 72, 72);
                bool selected = fd.Selected == s;
                if (GUI.Button(slotRect, GUIContent.none, selected ? slotOn : slot)) fd.SelectSpell(i);
                GUI.DrawTexture(new Rect(slotRect.x + 8, slotRect.y + 8, 56, 56), Art.SpellIcon(s));

                string problem = placing ? "Aguarde o início da luta" : fd.SpellProblem(s);
                if (problem != null || !myTurn)
                {
                    GUI.color = new Color(0f, 0f, 0f, 0.5f);
                    GUI.DrawTexture(new Rect(slotRect.x + 6, slotRect.y + 6, 60, 60), Art.White);
                    GUI.color = Color.white;
                }
                int cd = player.CooldownOf(s);
                if (cd > 0) ShadowLabel(slotRect, cd.ToString(), bigNumber);
                ShadowLabel(new Rect(slotRect.x + 5, slotRect.y + 2, 20, 18), (i + 1).ToString(), keyLabel);
                ShadowLabel(new Rect(slotRect.x + 44, slotRect.y + 52, 24, 18), s.ApCost.ToString(), new GUIStyle(keyLabel) { alignment = TextAnchor.LowerRight, normal = { textColor = new Color(0.6f, 0.8f, 1f) } });

                if (slotRect.Contains(mouse))
                {
                    var sb = new StringBuilder($"<b>{s.Name}</b>  <color=#9ad0ff>{s.ApCost} PA</color>  •  alcance {s.RangeText}");
                    if (s.Effect == SpellEffect.Damage) sb.Append($"\nDano {s.Min}-{s.Max} ({ElementName(s.Element)})");
                    if (s.Effect == SpellEffect.Heal) sb.Append($"\nCura {s.Min}-{s.Max}");
                    if (s.Area > 0) sb.Append("\nÁrea em cruz");
                    if (s.InLineOnly) sb.Append("\nSó em linha reta");
                    if (!s.NeedsSight) sb.Append("\nNão precisa de linha de visão");
                    if (s.Cooldown > 0) sb.Append($"\nRecarga: {s.Cooldown} turno(s)");
                    sb.Append($"\n<i>{s.Description}</i>");
                    if (problem != null && !placing) sb.Append($"\n<color=#ff8a80>{problem}</color>");
                    tooltip = sb.ToString();
                }
            }

            var br = new Rect(r.x + w - 140, r.y + 14, 126, 46);
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
                    Bar(new Rect(br.x, r.y + 70, br.width, 14), fd.TurnTimeLeft / FightDirector.TurnTime, new Color(0.95f, 0.72f, 0.2f), $"{Mathf.CeilToInt(Mathf.Max(0f, fd.TurnTimeLeft))}s");
            }
        }

        void DrawOverheadBars(FightDirector fd)
        {
            foreach (var kv in fd.Actors)
            {
                var f = kv.Key;
                if (!f.IsAlive) continue;
                var g = WorldToGui(kv.Value.HeadPosition);
                var color = f.Team == Team.Players ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.35f, 0.3f);
                Bar(new Rect(g.x - 24, g.y - 8, 48, 7), (float)f.Hp / f.MaxHp, color, "");
            }
        }

        void DrawPopups()
        {
            foreach (var p in game.Popups)
            {
                var g = WorldToGui(p.World + Vector3.up * (p.Age * 0.45f));
                float a = 1f - Mathf.Clamp01((p.Age - 0.8f) / 0.5f);
                var style = p.Big ? popupBig : popup;
                var rect = new Rect(g.x - 80, g.y - 40, 160, 30);
                style.normal.textColor = new Color(0f, 0f, 0f, 0.7f * a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), p.Text, style);
                style.normal.textColor = new Color(p.Color.r, p.Color.g, p.Color.b, a);
                GUI.Label(rect, p.Text, style);
            }
        }

        void DrawTooltip(string content)
        {
            var gc = new GUIContent(content);
            float w = 290f;
            float h = smallLight.CalcHeight(gc, w - 20) + 16;
            float x = Mathf.Min(mouse.x + 18, vw - w - 6);
            float y = Mathf.Min(mouse.y + 18, VH - h - 6);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, dark);
            GUI.Label(new Rect(x + 10, y + 8, w - 20, h - 16), gc, smallLight);
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
            ShadowLabel(new Rect(r.x + 4, r.y, 36, r.height), value, new GUIStyle(bigNumber) { fontSize = 24, normal = { textColor = color } });
            GUI.Label(new Rect(r.x + 38, r.y, 30, r.height), label, new GUIStyle(smallLight) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold });
        }

        void ShadowLabel(Rect r, string content, GUIStyle style)
        {
            var c = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.55f * c.a);
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

        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            panel = new GUIStyle
            {
                normal = { background = Art.UiBox("panel", Art.Hex("#f5ead0"), Art.Hex("#6b4a2b"), Art.Hex("#e3d2ad"), 7f, 2.5f) },
                border = new RectOffset(9, 9, 9, 9),
            };
            dark = new GUIStyle
            {
                normal = { background = Art.UiBox("dark", new Color(0.14f, 0.11f, 0.09f, 0.9f), Art.Hex("#b08d57"), new Color(0.09f, 0.07f, 0.06f, 0.92f), 7f, 2f) },
                border = new RectOffset(9, 9, 9, 9),
            };
            button = new GUIStyle(GUI.skin.button)
            {
                normal = { background = Art.UiBox("btn", Art.Hex("#e6953f"), Art.Hex("#5a3515"), Art.Hex("#b8661d")), textColor = Color.white },
                hover = { background = Art.UiBox("btn_h", Art.Hex("#f5ab58"), Art.Hex("#5a3515"), Art.Hex("#c97a2c")), textColor = Color.white },
                active = { background = Art.UiBox("btn_a", Art.Hex("#b8661d"), Art.Hex("#5a3515"), Art.Hex("#9a5516")), textColor = new Color(1f, 0.95f, 0.85f) },
                focused = { background = Art.UiBox("btn", Art.Hex("#e6953f"), Art.Hex("#5a3515"), Art.Hex("#b8661d")), textColor = Color.white },
                border = new RectOffset(9, 9, 9, 9),
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            bigButton = new GUIStyle(button) { fontSize = 20 };
            smallButton = new GUIStyle(button) { fontSize = 13 };
            slot = new GUIStyle
            {
                normal = { background = Art.UiBox("slot", Art.Hex("#4c3828"), Art.Hex("#c9a86a"), Art.Hex("#2e2219"), 7f, 2f) },
                hover = { background = Art.UiBox("slot_h", Art.Hex("#5e4632"), Art.Hex("#f0d08a"), Art.Hex("#3a2a1f"), 7f, 2f) },
                active = { background = Art.UiBox("slot_on", Art.Hex("#7a5530"), Art.Hex("#ffd54f"), Art.Hex("#4d3520"), 7f, 3f) },
                border = new RectOffset(9, 9, 9, 9),
            };
            slotOn = new GUIStyle
            {
                normal = { background = Art.UiBox("slot_on", Art.Hex("#7a5530"), Art.Hex("#ffd54f"), Art.Hex("#4d3520"), 7f, 3f) },
                hover = { background = Art.UiBox("slot_on", Art.Hex("#7a5530"), Art.Hex("#ffd54f"), Art.Hex("#4d3520"), 7f, 3f) },
                active = { background = Art.UiBox("slot_on", Art.Hex("#7a5530"), Art.Hex("#ffd54f"), Art.Hex("#4d3520"), 7f, 3f) },
                border = new RectOffset(9, 9, 9, 9),
            };
            text = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true, normal = { textColor = Brown } };
            textLight = new GUIStyle(text) { normal = { textColor = Cream } };
            textCenter = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(text) { fontSize = 13 };
            smallLight = new GUIStyle(small) { normal = { textColor = Cream } };
            smallCenter = new GUIStyle(smallLight) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            header = new GUIStyle(text) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = false };
            title = new GUIStyle(text) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = new Color(1f, 0.84f, 0.4f) } };
            subtitle = new GUIStyle(text) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = Cream } };
            field = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 17,
                padding = new RectOffset(10, 10, 6, 6),
                normal = { background = Art.UiBox("field", Art.Hex("#fffaf0"), Art.Hex("#8a6a45"), Art.Hex("#f3ead8"), 5f, 2f), textColor = Brown },
                focused = { background = Art.UiBox("field_f", Art.Hex("#ffffff"), Art.Hex("#e6953f"), Art.Hex("#f8f0e0"), 5f, 2f), textColor = Brown },
                hover = { background = Art.UiBox("field", Art.Hex("#fffaf0"), Art.Hex("#8a6a45"), Art.Hex("#f3ead8"), 5f, 2f), textColor = Brown },
                border = new RectOffset(8, 8, 8, 8),
            };
            bigNumber = new GUIStyle(text) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, normal = { textColor = Color.white } };
            keyLabel = new GUIStyle(text) { fontSize = 12, fontStyle = FontStyle.Bold, wordWrap = false, normal = { textColor = Cream } };
            popup = new GUIStyle(text) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            popupBig = new GUIStyle(popup) { fontSize = 24 };
        }
    }
}
