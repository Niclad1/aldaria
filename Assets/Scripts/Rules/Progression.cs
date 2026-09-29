using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    public static class Progression
    {
        public const int MaxLevel = 50;

        public static int XpToNext(int level) => 40 + level * level * 20;
        public static int MaxHp(ClassDef cls, int level) => cls.BaseHp + cls.HpPerLevel * (level - 1);
        public static int Power(int level) => (level - 1) * 4;

        public static int XpReward(IEnumerable<Fighter> monsters, int playerLevel)
        {
            int xp = 0;
            int gap = 0;
            foreach (var m in monsters)
            {
                xp += m.Level * 12 + m.MaxHp / 3;
                gap = Math.Max(gap, playerLevel - m.Level);
            }
            // Monstros muito mais fracos que o jogador rendem menos.
            if (gap > 5) xp = xp * 5 / gap;
            return Math.Max(1, xp);
        }

        public static int GoldReward(IEnumerable<Fighter> monsters, Random rng)
        {
            int gold = 0;
            foreach (var m in monsters) gold += rng.Next(3, 8) * m.Level;
            return gold;
        }

        /// <summary>Sorteia os itens deixados pelos monstros derrotados.</summary>
        public static List<ItemStack> RollDrops(IEnumerable<Fighter> monsters, Random rng)
        {
            var drops = new List<ItemStack>();
            foreach (var m in monsters)
            {
                var def = Catalog.MonsterById(m.Visual);
                foreach (var d in def.Drops)
                {
                    if (rng.Next(100) >= d.Chance) continue;
                    int n = rng.Next(d.Min, d.Max + 1);
                    var existing = drops.Find(s => s.Id == d.ItemId);
                    if (existing != null) existing.Count += n;
                    else drops.Add(new ItemStack { Id = d.ItemId, Count = n });
                }
            }
            return drops;
        }
    }

    /// <summary>Tudo o que é salvo do personagem. Campos públicos para serializar em JSON.</summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public string Name = "Aventureiro";
        public string ClassId = "guardiao";
        public int Level = 1;
        public int Xp;
        public int Gold;
        public int Hp;
        public int MapX;
        public int MapY;
        public int CellX = -1;
        public int CellY = -1;
        public int Victories;
        public List<ItemStack> Inventory = new List<ItemStack>();
        /// <summary>Id do item em cada espaço (índice = ItemSlot). Vazio = nada equipado.</summary>
        public string[] Equipment = new string[SlotCount];
        public List<QuestProgress> Quests = new List<QuestProgress>();

        public const int SlotCount = 7;

        public ClassDef Class => Catalog.ClassById(ClassId);
        public int MaxHp => Progression.MaxHp(Class, Level) + EquipmentStats.Hp;

        /// <summary>Cria um personagem novo com o equipamento inicial da classe.</summary>
        public static PlayerProfile Create(string name, string classId)
        {
            var p = new PlayerProfile { Name = name, ClassId = classId };
            var cls = p.Class;
            if (!string.IsNullOrEmpty(cls.StarterWeapon))
            {
                p.AddItem(cls.StarterWeapon, 1);
                p.Equip(cls.StarterWeapon);
            }
            p.AddItem("pocao_pequena", 3);
            p.Hp = p.MaxHp;
            return p;
        }

        /// <summary>Corrige campos que podem vir nulos de saves antigos.</summary>
        public void Validate()
        {
            if (Inventory == null) Inventory = new List<ItemStack>();
            if (Quests == null) Quests = new List<QuestProgress>();
            if (Equipment == null || Equipment.Length != SlotCount) Array.Resize(ref Equipment, SlotCount);
            Inventory.RemoveAll(s => s == null || s.Count <= 0 || Catalog.ItemById(s.Id) == null);
            for (int i = 0; i < SlotCount; i++)
                if (!string.IsNullOrEmpty(Equipment[i]) && Catalog.ItemById(Equipment[i]) == null) Equipment[i] = null;
            if (Hp <= 0 || Hp > MaxHp) Hp = MaxHp;
        }

        // ---------------------------------------------------------------- experiência

        /// <summary>Adiciona experiência e devolve quantos níveis foram ganhos.</summary>
        public int GainXp(int amount)
        {
            int levels = 0;
            Xp += amount;
            while (Level < Progression.MaxLevel && Xp >= Progression.XpToNext(Level))
            {
                Xp -= Progression.XpToNext(Level);
                Level++;
                levels++;
            }
            if (levels > 0) Hp = MaxHp;
            return levels;
        }

        // ---------------------------------------------------------------- inventário

        public int CountItem(string id)
        {
            var s = Inventory.Find(x => x.Id == id);
            return s != null ? s.Count : 0;
        }

        public void AddItem(string id, int count)
        {
            if (count <= 0 || Catalog.ItemById(id) == null) return;
            var s = Inventory.Find(x => x.Id == id);
            if (s != null) s.Count += count;
            else Inventory.Add(new ItemStack { Id = id, Count = count });
        }

        public bool RemoveItem(string id, int count)
        {
            var s = Inventory.Find(x => x.Id == id);
            if (s == null || s.Count < count) return false;
            s.Count -= count;
            if (s.Count <= 0)
            {
                Inventory.Remove(s);
                // Se era a última unidade de algo equipado, desequipa.
                for (int i = 0; i < SlotCount; i++)
                    if (Equipment[i] == id) Equipment[i] = null;
            }
            return true;
        }

        public bool IsEquipped(string id) => Array.IndexOf(Equipment, id) >= 0;

        public Stats EquipmentStats
        {
            get
            {
                var total = new Stats();
                if (Equipment == null) return total;
                foreach (var id in Equipment)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var it = Catalog.ItemById(id);
                    if (it != null) total += it.Stats;
                }
                return total;
            }
        }

        /// <summary>Equipa um item do inventário. Devolve o motivo se não der.</summary>
        public string Equip(string id)
        {
            var it = Catalog.ItemById(id);
            if (it == null || it.Kind != ItemKind.Equipment) return "Esse item não é equipável";
            if (CountItem(id) <= 0) return "Você não tem esse item";
            if (Level < it.Level) return $"Precisa do nível {it.Level}";
            Equipment[(int)it.Slot] = id;
            Hp = Math.Min(Hp, MaxHp);
            return null;
        }

        public void Unequip(ItemSlot slot)
        {
            Equipment[(int)slot] = null;
            Hp = Math.Min(Hp, MaxHp);
        }

        /// <summary>Usa um consumível. Devolve o motivo se não der.</summary>
        public string Use(string id)
        {
            var it = Catalog.ItemById(id);
            if (it == null || it.Kind != ItemKind.Consumable) return "Esse item não pode ser usado";
            if (Hp >= MaxHp) return "Sua vida já está cheia";
            if (!RemoveItem(id, 1)) return "Você não tem esse item";
            Hp = Math.Min(MaxHp, Hp + it.Heal);
            return null;
        }

        public string Buy(string id)
        {
            var it = Catalog.ItemById(id);
            if (it == null) return "Item inexistente";
            if (Gold < it.Price) return "Ouro insuficiente";
            Gold -= it.Price;
            AddItem(id, 1);
            return null;
        }

        public string Sell(string id)
        {
            var it = Catalog.ItemById(id);
            if (it == null || CountItem(id) <= 0) return "Você não tem esse item";
            if (IsEquipped(id) && CountItem(id) == 1) return "Desequipe antes de vender";
            RemoveItem(id, 1);
            Gold += it.SellPrice;
            return null;
        }
    }
}
