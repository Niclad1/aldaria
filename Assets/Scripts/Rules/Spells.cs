using System.Collections.Generic;

namespace Aldaria.Rules
{
    /// <summary>Os quatro elementos clássicos que dão cor aos feitiços.</summary>
    public enum Element
    {
        Neutral,
        Earth,
        Fire,
        Water,
        Air
    }

    public enum SpellEffect
    {
        Damage,
        Heal,
        Teleport,
        Buff
    }

    public enum SpellTarget
    {
        /// <summary>Qualquer célula dentro do alcance (feitiços de área).</summary>
        AnyCell,
        /// <summary>Precisa haver um lutador na célula.</summary>
        Fighter,
        /// <summary>Célula livre e caminhável (teleporte).</summary>
        EmptyCell,
        /// <summary>Só o próprio lançador.</summary>
        Self
    }

    public enum BuffStat
    {
        Damage,
        Ap,
        Mp
    }

    public sealed class SpellDef
    {
        public string Id;
        public string Name;
        public string Description;
        public Element Element;
        public SpellEffect Effect = SpellEffect.Damage;
        public SpellTarget Target = SpellTarget.Fighter;
        public int ApCost = 3;
        public int MinRange = 1;
        public int MaxRange = 1;
        public bool NeedsSight = true;
        public bool InLineOnly;
        /// <summary>Raio (em passos) da área de efeito ao redor da célula alvo. 0 = só a célula.</summary>
        public int Area;
        public int Min;
        public int Max;
        /// <summary>Quantas células o alvo é empurrado para longe do lançador.</summary>
        public int Push;
        /// <summary>PM retirados do alvo no próximo turno dele.</summary>
        public int MpSteal;
        public BuffStat BuffStat;
        public int BuffTurns;
        public int Cooldown;
        public int MaxCastsPerTurn = 2;

        public string RangeText => MinRange == MaxRange ? $"{MaxRange}" : $"{MinRange}-{MaxRange}";
    }

    public sealed class Buff
    {
        public BuffStat Stat;
        public int Value;
        public int TurnsLeft;
        public string Source;
    }

    public sealed class ClassDef
    {
        public string Id;
        public string Name;
        public string Role;
        public string Description;
        public int BaseHp;
        public int HpPerLevel;
        public int Ap = 6;
        public int Mp = 3;
        public int Initiative;
        public List<SpellDef> Spells = new List<SpellDef>();
    }

    public sealed class MonsterDef
    {
        public string Id;
        public string Name;
        public int BaseHp;
        public int HpPerLevel;
        public int Ap = 6;
        public int Mp = 3;
        public int Initiative;
        public List<SpellDef> Spells = new List<SpellDef>();
    }
}
