using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum Team
    {
        Players,
        Monsters
    }

    /// <summary>Um participante de uma luta (jogador ou monstro).</summary>
    public sealed class Fighter
    {
        public int Id;
        public string Name;
        /// <summary>Id da classe ou do monstro; a camada visual usa isso para escolher o sprite.</summary>
        public string Visual;
        public Team Team;
        public bool IsAI;
        public int Level = 1;
        public int MaxHp;
        public int Hp;
        public int BaseAp = 6;
        public int BaseMp = 3;
        public int Ap;
        public int Mp;
        public int Initiative;
        /// <summary>Bônus percentual em dano e cura.</summary>
        public int Power;
        public Cell Cell;
        public List<SpellDef> Spells = new List<SpellDef>();
        public readonly List<Buff> Buffs = new List<Buff>();
        public readonly Dictionary<string, int> Cooldowns = new Dictionary<string, int>();
        public readonly Dictionary<string, int> CastsThisTurn = new Dictionary<string, int>();

        public bool IsAlive => Hp > 0;

        public int BuffTotal(BuffStat stat)
        {
            int total = 0;
            foreach (var b in Buffs)
                if (b.Stat == stat) total += b.Value;
            return total;
        }

        public int CooldownOf(SpellDef s) => Cooldowns.TryGetValue(s.Id, out int n) ? n : 0;
        public int CastsOf(SpellDef s) => CastsThisTurn.TryGetValue(s.Id, out int n) ? n : 0;

        public static Fighter FromClass(int id, string name, ClassDef cls, int level, int hp)
        {
            int maxHp = Progression.MaxHp(cls, level);
            return new Fighter
            {
                Id = id,
                Name = name,
                Visual = cls.Id,
                Team = Team.Players,
                Level = level,
                MaxHp = maxHp,
                Hp = hp <= 0 ? maxHp : System.Math.Min(hp, maxHp),
                BaseAp = cls.Ap,
                BaseMp = cls.Mp,
                Initiative = cls.Initiative + level * 5,
                Power = Progression.Power(level),
                Spells = cls.Spells,
            };
        }

        public static Fighter FromMonster(int id, MonsterDef def, int level)
        {
            int maxHp = def.BaseHp + def.HpPerLevel * (level - 1);
            return new Fighter
            {
                Id = id,
                Name = def.Name,
                Visual = def.Id,
                Team = Team.Monsters,
                IsAI = true,
                Level = level,
                MaxHp = maxHp,
                Hp = maxHp,
                BaseAp = def.Ap,
                BaseMp = def.Mp,
                Initiative = def.Initiative + level * 3,
                Power = (level - 1) * 5,
                Spells = def.Spells,
            };
        }
    }
}
