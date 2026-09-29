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
            foreach (var m in monsters) xp += m.Level * 12 + m.MaxHp / 3;
            // Monstros muito mais fracos que o jogador rendem menos.
            int gap = 0;
            foreach (var m in monsters) gap = Math.Max(gap, playerLevel - m.Level);
            if (gap > 5) xp = xp * 5 / gap;
            return Math.Max(1, xp);
        }

        public static int GoldReward(IEnumerable<Fighter> monsters, Random rng)
        {
            int gold = 0;
            foreach (var m in monsters) gold += rng.Next(3, 8) * m.Level;
            return gold;
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

        public ClassDef Class => Catalog.ClassById(ClassId);
        public int MaxHp => Progression.MaxHp(Class, Level);

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
    }
}
