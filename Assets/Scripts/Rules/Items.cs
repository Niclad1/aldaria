using System;

namespace Aldaria.Rules
{
    public enum ItemKind
    {
        Equipment,
        Consumable,
        Resource
    }

    /// <summary>Espaços de equipamento do personagem. A ordem é usada como índice no perfil salvo.</summary>
    public enum ItemSlot
    {
        Weapon,
        Hat,
        Cloak,
        Amulet,
        Ring,
        Belt,
        Boots
    }

    /// <summary>Bônus somados de todos os equipamentos.</summary>
    public struct Stats
    {
        public int Hp;
        public int Power;
        public int Damage;
        public int Ap;
        public int Mp;
        public int Initiative;

        public static Stats operator +(Stats a, Stats b) => new Stats
        {
            Hp = a.Hp + b.Hp,
            Power = a.Power + b.Power,
            Damage = a.Damage + b.Damage,
            Ap = a.Ap + b.Ap,
            Mp = a.Mp + b.Mp,
            Initiative = a.Initiative + b.Initiative,
        };
    }

    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemKind Kind;
        public ItemSlot Slot;
        public int Level = 1;
        /// <summary>0 comum, 1 incomum, 2 raro, 3 lendário.</summary>
        public int Rarity;
        /// <summary>Preço de compra na loja. A venda rende 1/4 disso.</summary>
        public int Price;
        public Stats Stats;
        /// <summary>Vida recuperada ao usar (consumíveis).</summary>
        public int Heal;

        public int SellPrice => Math.Max(1, Price / 4);

        public string StatsText()
        {
            var sb = new System.Text.StringBuilder();
            void Add(int v, string label)
            {
                if (v == 0) return;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(v > 0 ? "+" : "").Append(v).Append(' ').Append(label);
            }
            Add(Stats.Hp, "PV");
            Add(Stats.Power, "Poder");
            Add(Stats.Damage, "Dano");
            Add(Stats.Ap, "PA");
            Add(Stats.Mp, "PM");
            Add(Stats.Initiative, "Iniciativa");
            if (Heal > 0)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("Recupera ").Append(Heal).Append(" PV");
            }
            return sb.ToString();
        }

        public static string SlotName(ItemSlot s)
        {
            switch (s)
            {
                case ItemSlot.Weapon: return "Arma";
                case ItemSlot.Hat: return "Chapéu";
                case ItemSlot.Cloak: return "Capa";
                case ItemSlot.Amulet: return "Amuleto";
                case ItemSlot.Ring: return "Anel";
                case ItemSlot.Belt: return "Cinto";
                default: return "Botas";
            }
        }

        public static string RarityName(int r) => r == 0 ? "Comum" : r == 1 ? "Incomum" : r == 2 ? "Raro" : "Lendário";
    }

    [Serializable]
    public sealed class ItemStack
    {
        public string Id;
        public int Count;
    }
}
