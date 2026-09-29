using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum ObjectiveKind
    {
        /// <summary>Derrotar monstros (Target = id do monstro).</summary>
        Kill,
        /// <summary>Ter itens no inventário na hora de entregar (Target = id do item). Os itens são entregues.</summary>
        Collect,
        /// <summary>Conversar com um NPC (Target = id do NPC).</summary>
        Talk,
        /// <summary>Chegar a um mapa (Target = "x,y").</summary>
        Visit
    }

    public sealed class QuestObjective
    {
        public ObjectiveKind Kind;
        public string Target;
        public int Count = 1;
        public string Text;
    }

    public sealed class QuestDef
    {
        public string Id;
        public string Name;
        public string Giver;
        public string TurnIn;
        public int MinLevel = 1;
        public string Requires;
        public string Offer;
        public string Progress;
        public string Complete;
        public List<QuestObjective> Objectives = new List<QuestObjective>();
        public int RewardXp;
        public int RewardGold;
        public List<ItemStack> RewardItems = new List<ItemStack>();
    }

    public sealed class NpcDef
    {
        public string Id;
        public string Name;
        public string Title;
        public int MapX;
        public int MapY;
        public Cell Cell;
        public string Greeting;
        public List<string> ShopItems = new List<string>();
        public bool IsShop => ShopItems.Count > 0;
    }

    [Serializable]
    public sealed class QuestProgress
    {
        public string Id;
        public bool Done;
        public int[] Counts = new int[0];
    }

    public enum QuestStatus
    {
        Locked,
        Available,
        Active,
        ReadyToTurnIn,
        Done
    }

    /// <summary>Regras das missões aplicadas sobre o perfil do jogador.</summary>
    public static class QuestLog
    {
        public static QuestProgress Get(PlayerProfile p, string questId) => p.Quests.Find(q => q.Id == questId);

        public static QuestStatus StatusOf(PlayerProfile p, QuestDef q)
        {
            var prog = Get(p, q.Id);
            if (prog != null)
            {
                if (prog.Done) return QuestStatus.Done;
                return IsComplete(p, q, prog) ? QuestStatus.ReadyToTurnIn : QuestStatus.Active;
            }
            if (p.Level < q.MinLevel) return QuestStatus.Locked;
            if (!string.IsNullOrEmpty(q.Requires))
            {
                var req = Get(p, q.Requires);
                if (req == null || !req.Done) return QuestStatus.Locked;
            }
            return QuestStatus.Available;
        }

        public static int ObjectiveCount(PlayerProfile p, QuestDef q, QuestProgress prog, int i)
        {
            var o = q.Objectives[i];
            if (o.Kind == ObjectiveKind.Collect) return Math.Min(o.Count, p.CountItem(o.Target));
            return i < prog.Counts.Length ? Math.Min(o.Count, prog.Counts[i]) : 0;
        }

        public static bool IsComplete(PlayerProfile p, QuestDef q, QuestProgress prog)
        {
            for (int i = 0; i < q.Objectives.Count; i++)
                if (ObjectiveCount(p, q, prog, i) < q.Objectives[i].Count) return false;
            return true;
        }

        public static bool Accept(PlayerProfile p, QuestDef q)
        {
            if (StatusOf(p, q) != QuestStatus.Available) return false;
            p.Quests.Add(new QuestProgress { Id = q.Id, Counts = new int[q.Objectives.Count] });
            return true;
        }

        /// <summary>Entrega a missão e aplica as recompensas. Devolve os níveis ganhos (ou -1 se não deu para entregar).</summary>
        public static int TurnIn(PlayerProfile p, QuestDef q)
        {
            if (StatusOf(p, q) != QuestStatus.ReadyToTurnIn) return -1;
            foreach (var o in q.Objectives)
                if (o.Kind == ObjectiveKind.Collect) p.RemoveItem(o.Target, o.Count);
            Get(p, q.Id).Done = true;
            p.Gold += q.RewardGold;
            foreach (var it in q.RewardItems) p.AddItem(it.Id, it.Count);
            return p.GainXp(q.RewardXp);
        }

        public static void OnKill(PlayerProfile p, string monsterId) => Advance(p, ObjectiveKind.Kill, monsterId);
        public static void OnTalk(PlayerProfile p, string npcId) => Advance(p, ObjectiveKind.Talk, npcId);
        public static void OnVisit(PlayerProfile p, int mapX, int mapY) => Advance(p, ObjectiveKind.Visit, $"{mapX},{mapY}");

        static void Advance(PlayerProfile p, ObjectiveKind kind, string target)
        {
            foreach (var prog in p.Quests)
            {
                if (prog.Done) continue;
                var q = Catalog.QuestById(prog.Id);
                if (q == null) continue;
                if (prog.Counts.Length != q.Objectives.Count) Array.Resize(ref prog.Counts, q.Objectives.Count);
                for (int i = 0; i < q.Objectives.Count; i++)
                {
                    var o = q.Objectives[i];
                    if (o.Kind == kind && o.Target == target && prog.Counts[i] < o.Count) prog.Counts[i]++;
                }
            }
        }
    }
}
