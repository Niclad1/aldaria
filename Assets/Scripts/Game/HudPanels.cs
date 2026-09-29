using System.Collections.Generic;
using System.Text;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>Painéis da exploração: acompanhamento de missões, inventário, diário, conversa com NPC e loja.</summary>
    public sealed partial class Hud
    {
        Vector2 bagScroll, questScroll, shopScroll, sellScroll;

        static readonly string[] RarityColors = { "#e8e0d0", "#7fd04a", "#5aa8ff", "#ff9a3a" };

        // ================================================================== acompanhamento (canto direito)

        void DrawQuestTracker()
        {
            var p = game.Profile;
            var active = p.Quests.FindAll(q => !q.Done);
            if (active.Count == 0 || game.OpenPanel != UiPanel.None) return;
            var sb = new StringBuilder();
            int shown = 0;
            foreach (var prog in active)
            {
                var q = Catalog.QuestById(prog.Id);
                if (q == null || shown >= 3) continue;
                shown++;
                bool ready = QuestLog.IsComplete(p, q, prog);
                sb.Append(shown > 1 ? "\n" : "").Append($"<b>{q.Name}</b>");
                if (ready) sb.Append($"\n  <color=#ffd166>Volte para {Catalog.NpcById(q.TurnIn).Name}</color>");
                else
                    for (int i = 0; i < q.Objectives.Count; i++)
                        sb.Append($"\n  {q.Objectives[i].Text}: {QuestLog.ObjectiveCount(p, q, prog, i)}/{q.Objectives[i].Count}");
            }
            var content = new GUIContent(sb.ToString());
            float h = smallLight.CalcHeight(content, 236) + 44;
            var r = Panel(new Rect(vw - 276, 70, 264, h), dark);
            GUI.Label(new Rect(r.x + 14, r.y + 8, 240, 22), "Missões", new GUIStyle(smallLight) { font = Art.UiFont(true), normal = { textColor = GoldText } });
            GUI.Label(new Rect(r.x + 14, r.y + 32, 240, h - 36), content, smallLight);
        }

        // ================================================================== inventário

        void DrawInventory()
        {
            var p = game.Profile;
            var r = Panel(new Rect(vw / 2f - 340, 80, 680, 520), panel);
            PanelHeader(r, "Inventário");

            // Equipamento
            var left = new Rect(r.x + 26, r.y + 64, 270, 430);
            GUI.Label(new Rect(left.x, left.y, left.width, 24), "Equipamento", header);
            var figure = new Rect(left.x + 70, left.y + 34, 130, 200);
            GUI.Box(figure, GUIContent.none, slot);
            DrawFull(new Rect(figure.x + 6, figure.y + 6, figure.width - 12, figure.height - 12), Art.Character(p.ClassId));
            var slotPos = new Dictionary<ItemSlot, Vector2>
            {
                [ItemSlot.Hat] = new Vector2(left.x, left.y + 34), [ItemSlot.Amulet] = new Vector2(left.x, left.y + 104), [ItemSlot.Weapon] = new Vector2(left.x, left.y + 174),
                [ItemSlot.Cloak] = new Vector2(left.x + 210, left.y + 34), [ItemSlot.Ring] = new Vector2(left.x + 210, left.y + 104), [ItemSlot.Belt] = new Vector2(left.x + 210, left.y + 174),
                [ItemSlot.Boots] = new Vector2(left.x + 105, left.y + 244),
            };
            foreach (var kv in slotPos)
            {
                var sr = new Rect(kv.Value.x, kv.Value.y, 60, 60);
                var item = Catalog.ItemById(p.Equipment[(int)kv.Key]);
                if (GUI.Button(sr, GUIContent.none, slot) && item != null) game.UnequipSlot(kv.Key);
                if (item != null)
                {
                    GUI.DrawTexture(new Rect(sr.x + 6, sr.y + 6, 48, 48), Art.ItemIcon(item));
                    if (sr.Contains(mouse)) tooltip = ItemTooltip(item, "Clique para desequipar");
                }
                else
                {
                    GUI.Label(sr, ItemDef.SlotName(kv.Key), new GUIStyle(smallCenter) { fontSize = 11, wordWrap = true, normal = { textColor = new Color(1, 1, 1, 0.4f) } });
                }
            }
            var st = p.EquipmentStats;
            var stats = $"<b>PV</b> {p.Hp}/{p.MaxHp}    <b>Poder</b> {Progression.Power(p.Level) + st.Power}%\n" +
                        $"<b>PA</b> {p.Class.Ap + st.Ap}    <b>PM</b> {p.Class.Mp + st.Mp}    <b>Dano</b> +{st.Damage}\n" +
                        $"<b>Iniciativa</b> {p.Class.Initiative + p.Level * 5 + st.Initiative}";
            GUI.Label(new Rect(left.x, left.y + 318, left.width, 80), stats, text);

            // Mochila
            var right = new Rect(r.x + 316, r.y + 64, 340, 430);
            GUI.Label(new Rect(right.x, right.y, 200, 24), "Mochila", header);
            GUI.Label(new Rect(right.x + 160, right.y + 2, 180, 24), $"<color=#9a6a10><b>{p.Gold}</b></color> de ouro", new GUIStyle(text) { alignment = TextAnchor.UpperRight });
            var items = new List<ItemStack>(p.Inventory);
            int cols = 5;
            float cell = 64;
            int rows = Mathf.Max(5, (items.Count + cols - 1) / cols);
            var view = new Rect(right.x, right.y + 34, right.width, 340);
            GUI.Box(view, GUIContent.none, slot);
            bagScroll = GUI.BeginScrollView(new Rect(view.x + 6, view.y + 6, view.width - 12, view.height - 12), bagScroll, new Rect(0, 0, cols * cell, rows * cell));
            var local = mouse - new Vector2(view.x + 6, view.y + 6) + bagScroll;
            for (int i = 0; i < rows * cols; i++)
            {
                var sr = new Rect((i % cols) * cell + 2, (i / cols) * cell + 2, cell - 4, cell - 4);
                if (i >= items.Count)
                {
                    GUI.Box(sr, GUIContent.none, slot);
                    continue;
                }
                var stack = items[i];
                var it = Catalog.ItemById(stack.Id);
                bool equipped = p.IsEquipped(stack.Id);
                if (ItemSlotBox(sr, it, stack.Count, equipped))
                {
                    if (it.Kind == ItemKind.Equipment && !equipped) game.EquipItem(it.Id);
                    else if (it.Kind == ItemKind.Consumable) game.UseItem(it.Id);
                }
                if (sr.Contains(local) && view.Contains(mouse))
                    tooltip = ItemTooltip(it, it.Kind == ItemKind.Equipment ? (equipped ? "Equipado" : "Clique para equipar") : it.Kind == ItemKind.Consumable ? "Clique para usar" : "Recurso: venda na loja ou use em missões");
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(right.x, right.y + 382, right.width, 40), "Clique num equipamento para vestir. Poções recuperam vida fora da luta.", small);
        }

        /// <summary>Desenha um espaço com ícone, quantidade e cor de raridade. Devolve true se foi clicado.</summary>
        bool ItemSlotBox(Rect r, ItemDef it, int count, bool equipped)
        {
            bool clicked = GUI.Button(r, GUIContent.none, equipped ? slotOn : slot);
            if (it == null) return clicked;
            if (it.Rarity > 0)
            {
                ColorUtility.TryParseHtmlString(RarityColors[it.Rarity], out var rc);
                GUI.color = new Color(rc.r, rc.g, rc.b, 0.35f);
                GUI.DrawTexture(new Rect(r.x + 5, r.y + r.height - 9, r.width - 10, 4), Art.White);
                GUI.color = Color.white;
            }
            GUI.DrawTexture(new Rect(r.x + 6, r.y + 5, r.width - 12, r.height - 12), Art.ItemIcon(it));
            if (count > 1) ShadowLabel(new Rect(r.x, r.y + r.height - 22, r.width - 6, 20), count.ToString(), new GUIStyle(keyLabel) { alignment = TextAnchor.LowerRight });
            if (equipped) ShadowLabel(new Rect(r.x + 5, r.y + 2, 20, 18), "E", new GUIStyle(keyLabel) { normal = { textColor = GoldText } });
            return clicked;
        }

        string ItemTooltip(ItemDef it, string hint)
        {
            if (it == null) return "";
            var sb = new StringBuilder($"<b><color={RarityColors[it.Rarity]}>{it.Name}</color></b>");
            if (it.Kind == ItemKind.Equipment) sb.Append($"\n<color=#c9b48a>{ItemDef.SlotName(it.Slot)} • nível {it.Level} • {ItemDef.RarityName(it.Rarity)}</color>");
            else sb.Append($"\n<color=#c9b48a>{(it.Kind == ItemKind.Consumable ? "Consumível" : "Recurso")}</color>");
            var stats = it.StatsText();
            if (stats.Length > 0) sb.Append("\n<color=#9ae07a>").Append(stats).Append("</color>");
            sb.Append($"\n<i>{it.Description}</i>");
            if (game.Profile != null && it.Kind == ItemKind.Equipment && game.Profile.Level < it.Level) sb.Append($"\n<color=#ff8a80>Precisa do nível {it.Level}</color>");
            if (!string.IsNullOrEmpty(hint)) sb.Append($"\n<color=#ffd166>{hint}</color>");
            return sb.ToString();
        }

        // ================================================================== diário de missões

        void DrawQuestLog()
        {
            var p = game.Profile;
            var r = Panel(new Rect(vw / 2f - 300, 80, 600, 500), panel);
            PanelHeader(r, "Diário de missões");
            var sb = new StringBuilder();
            int active = 0, done = 0;
            foreach (var prog in p.Quests)
            {
                var q = Catalog.QuestById(prog.Id);
                if (q == null) continue;
                if (prog.Done)
                {
                    done++;
                    continue;
                }
                active++;
                bool ready = QuestLog.IsComplete(p, q, prog);
                var giver = Catalog.NpcById(q.TurnIn);
                sb.Append($"<size=18><b>{q.Name}</b></size>{(ready ? "  <color=#2f8a2f><b>(pronta!)</b></color>" : "")}\n");
                sb.Append($"<i>{q.Progress}</i>\n");
                for (int i = 0; i < q.Objectives.Count; i++)
                {
                    int c = QuestLog.ObjectiveCount(p, q, prog, i);
                    bool ok = c >= q.Objectives[i].Count;
                    sb.Append(ok ? "<color=#2f8a2f>✔ " : "• ").Append($"{q.Objectives[i].Text}: {c}/{q.Objectives[i].Count}").Append(ok ? "</color>\n" : "\n");
                }
                sb.Append($"<color=#7a5a3a>Entregar para {giver.Name} em [{giver.MapX},{giver.MapY}]</color>\n\n");
            }
            if (active == 0) sb.Append("Nenhuma missão em andamento.\nProcure moradores com <b>!</b> sobre a cabeça na Vila de Aldaria.");
            var content = new GUIContent(sb.ToString());
            float h = text.CalcHeight(content, 520);
            questScroll = GUI.BeginScrollView(new Rect(r.x + 30, r.y + 64, 548, 380), questScroll, new Rect(0, 0, 520, h));
            GUI.Label(new Rect(0, 0, 520, h), content, text);
            GUI.EndScrollView();
            GUI.Label(new Rect(r.x + 30, r.y + r.height - 44, 540, 24), $"Missões concluídas: {done} de {Catalog.Quests.Count}", small);
        }

        // ================================================================== conversa

        void DrawDialogue()
        {
            var npc = game.DialogueNpc;
            if (npc == null) return;
            var p = game.Profile;
            var r = Panel(new Rect(vw / 2f - 380, VH - 330, 760, 260), panel);
            if (GUI.Button(new Rect(r.xMax - 50, r.y + 12, 36, 34), "X", smallButton)) game.ClosePanel();

            GUI.Box(new Rect(r.x + 24, r.y + 24, 130, 130), GUIContent.none, slot);
            Art.DrawPortrait(new Rect(r.x + 30, r.y + 28, 118, 118), Art.Character("npc_" + npc.Id));
            GUI.Label(new Rect(r.x + 24, r.y + 160, 130, 26), npc.Name, new GUIStyle(header) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true });
            GUI.Label(new Rect(r.x + 24, r.y + 186, 130, 40), npc.Title, new GUIStyle(small) { alignment = TextAnchor.UpperCenter });

            var body = new Rect(r.x + 176, r.y + 24, r.width - 240, 110);
            var q = game.DialogueQuest;
            var buttons = new Rect(r.x + 176, r.y + 150, r.width - 200, 90);
            float bx = buttons.x;
            bool Btn(string label, float w)
            {
                bool clicked = GUI.Button(new Rect(bx, buttons.y, w, 42), label, smallButton);
                bx += w + 10;
                return clicked;
            }

            if (q == null)
            {
                GUI.Label(body, npc.Greeting, new GUIStyle(text) { fontSize = 17 });
                // Opções: missões deste NPC e loja, em duas colunas
                var options = new List<(string label, QuestDef quest)>();
                foreach (var quest in Catalog.Quests)
                {
                    var status = QuestLog.StatusOf(p, quest);
                    if (status == QuestStatus.Available && quest.Giver == npc.Id) options.Add(($"<color=#fff2a0>!</color>  {quest.Name}", quest));
                    else if (status == QuestStatus.ReadyToTurnIn && quest.TurnIn == npc.Id) options.Add(($"<color=#fff2a0>?</color>  {quest.Name}", quest));
                    else if (status == QuestStatus.Active && (quest.Giver == npc.Id || quest.TurnIn == npc.Id)) options.Add(($"…  {quest.Name}", quest));
                }
                if (npc.IsShop) options.Add(("Ver mercadorias", null));
                var optStyle = new GUIStyle(smallButton) { richText = true };
                for (int i = 0; i < options.Count && i < 4; i++)
                {
                    var br = new Rect(buttons.x + (i % 2) * 280, buttons.y - 14 + (i / 2) * 46, 270, 42);
                    if (!GUI.Button(br, options[i].label, optStyle)) continue;
                    if (options[i].quest != null) game.DialogueQuest = options[i].quest;
                    else game.OpenShop();
                }
                if (options.Count == 0) GUI.Label(new Rect(buttons.x, buttons.y, 400, 30), "<i>(Nenhuma missão para você agora.)</i>", small);
                return;
            }

            var st = QuestLog.StatusOf(p, q);
            var sb = new StringBuilder();
            sb.Append($"<b>{q.Name}</b>\n");
            if (st == QuestStatus.Available)
            {
                sb.Append(q.Offer);
                GUI.Label(body, sb.ToString(), text);
                GUI.Label(new Rect(body.x, body.yMax - 4, body.width, 22), "Recompensa: " + RewardText(q), new GUIStyle(small) { normal = { textColor = new Color(0.45f, 0.3f, 0.05f) } });
                if (Btn("Aceitar", 160)) game.AcceptQuest(q);
                if (Btn("Agora não", 160)) game.DialogueQuest = null;
            }
            else if (st == QuestStatus.ReadyToTurnIn && q.TurnIn == npc.Id)
            {
                sb.Append(q.Complete);
                GUI.Label(body, sb.ToString(), text);
                GUI.Label(new Rect(body.x, body.yMax - 4, body.width, 22), "Recompensa: " + RewardText(q), new GUIStyle(small) { normal = { textColor = new Color(0.45f, 0.3f, 0.05f) } });
                if (Btn("Entregar missão", 200)) game.TurnInQuest(q);
                if (Btn("Voltar", 130)) game.DialogueQuest = null;
            }
            else
            {
                sb.Append(q.Progress);
                var prog = QuestLog.Get(p, q.Id);
                if (prog != null)
                    for (int i = 0; i < q.Objectives.Count; i++)
                        sb.Append($"\n• {q.Objectives[i].Text}: {QuestLog.ObjectiveCount(p, q, prog, i)}/{q.Objectives[i].Count}");
                GUI.Label(body, sb.ToString(), text);
                if (Btn("Voltar", 130)) game.DialogueQuest = null;
            }
        }

        static string RewardText(QuestDef q)
        {
            var parts = new List<string>();
            if (q.RewardXp > 0) parts.Add($"{q.RewardXp} XP");
            if (q.RewardGold > 0) parts.Add($"{q.RewardGold} ouro");
            foreach (var it in q.RewardItems) parts.Add($"{(it.Count > 1 ? it.Count + "x " : "")}{Catalog.ItemById(it.Id).Name}");
            return string.Join(", ", parts);
        }

        // ================================================================== loja

        void DrawShop()
        {
            var npc = game.DialogueNpc;
            if (npc == null) return;
            var p = game.Profile;
            var r = Panel(new Rect(vw / 2f - 380, 70, 760, 540), panel);
            PanelHeader(r, $"Loja de {npc.Name}");
            GUI.Label(new Rect(r.x + 30, r.y + 58, 400, 24), $"Seu ouro: <color=#9a6a10><b>{p.Gold}</b></color>", text);

            // Comprar
            var buy = new Rect(r.x + 30, r.y + 92, 340, 420);
            GUI.Label(new Rect(buy.x, buy.y, 300, 24), "Comprar", header);
            shopScroll = GUI.BeginScrollView(new Rect(buy.x, buy.y + 32, buy.width, buy.height - 40), shopScroll, new Rect(0, 0, buy.width - 20, npc.ShopItems.Count * 62));
            var local = mouse - new Vector2(buy.x, buy.y + 32) + shopScroll;
            for (int i = 0; i < npc.ShopItems.Count; i++)
            {
                var it = Catalog.ItemById(npc.ShopItems[i]);
                var row = new Rect(0, i * 62, buy.width - 22, 56);
                ItemSlotBox(new Rect(row.x, row.y, 56, 56), it, 1, false);
                GUI.Label(new Rect(row.x + 64, row.y + 4, 170, 24), it.Name, new GUIStyle(text) { font = Art.UiFont(true), wordWrap = false, clipping = TextClipping.Clip });
                GUI.Label(new Rect(row.x + 64, row.y + 28, 170, 22), $"{it.Price} ouro", small);
                GUI.enabled = p.Gold >= it.Price;
                if (GUI.Button(new Rect(row.xMax - 92, row.y + 8, 90, 40), "Comprar", smallButton)) game.BuyItem(it.Id);
                GUI.enabled = true;
                if (row.Contains(local)) tooltip = ItemTooltip(it, "");
            }
            GUI.EndScrollView();

            // Vender
            var sell = new Rect(r.x + 396, r.y + 92, 340, 420);
            GUI.Label(new Rect(sell.x, sell.y, 300, 24), "Vender", header);
            var sellable = p.Inventory.FindAll(s => !(p.IsEquipped(s.Id) && s.Count == 1));
            sellScroll = GUI.BeginScrollView(new Rect(sell.x, sell.y + 32, sell.width, sell.height - 40), sellScroll, new Rect(0, 0, sell.width - 20, sellable.Count * 62));
            var local2 = mouse - new Vector2(sell.x, sell.y + 32) + sellScroll;
            for (int i = 0; i < sellable.Count; i++)
            {
                var stack = sellable[i];
                var it = Catalog.ItemById(stack.Id);
                var row = new Rect(0, i * 62, sell.width - 22, 56);
                ItemSlotBox(new Rect(row.x, row.y, 56, 56), it, stack.Count, false);
                GUI.Label(new Rect(row.x + 64, row.y + 4, 170, 24), it.Name, new GUIStyle(text) { font = Art.UiFont(true), wordWrap = false, clipping = TextClipping.Clip });
                GUI.Label(new Rect(row.x + 64, row.y + 28, 170, 22), $"{it.SellPrice} ouro cada", small);
                if (GUI.Button(new Rect(row.xMax - 92, row.y + 8, 90, 40), "Vender", smallButton)) game.SellItem(it.Id);
                if (row.Contains(local2)) tooltip = ItemTooltip(it, "");
            }
            GUI.EndScrollView();
            if (sellable.Count == 0) GUI.Label(new Rect(sell.x, sell.y + 40, sell.width, 60), "Nada para vender. Derrote monstros para conseguir recursos!", small);
        }

        void PanelHeader(Rect r, string label)
        {
            ShadowLabel(new Rect(r.x + 26, r.y + 18, r.width - 100, 34), label, new GUIStyle(header) { fontSize = 26, normal = { textColor = new Color(0.45f, 0.25f, 0.08f) } });
            if (GUI.Button(new Rect(r.xMax - 54, r.y + 16, 38, 36), "X", smallButton)) game.ClosePanel();
        }
    }
}
