using System.Collections.Generic;

namespace Aldaria.Rules
{
    public static partial class Catalog
    {
        public static readonly List<MonsterDef> Monsters = new List<MonsterDef>
        {
            // ---- Pradaria
            new MonsterDef
            {
                Id = "lanudo", Name = "Lanudo", BaseHp = 34, HpPerLevel = 6, Mp = 3, Initiative = 60,
                Spells = { new SpellDef { Id = "chifrada", Name = "Chifrada", Element = Element.Earth, ApCost = 3, Min = 5, Max = 8 } },
                Drops = { new DropDef { ItemId = "la", Chance = 70, Max = 2 }, new DropDef { ItemId = "gorro_la", Chance = 4 } },
            },
            new MonsterDef
            {
                Id = "pipio", Name = "Pipio", BaseHp = 22, HpPerLevel = 4, Mp = 4, Initiative = 140,
                Spells = { new SpellDef { Id = "bicada", Name = "Bicada", Element = Element.Air, ApCost = 3, MinRange = 1, MaxRange = 3, Min = 4, Max = 6 } },
                Drops = { new DropDef { ItemId = "pena", Chance = 70, Max = 2 }, new DropDef { ItemId = "botas_simples", Chance = 4 } },
            },
            new MonsterDef
            {
                Id = "cogumelo", Name = "Cogumelo Bravo", BaseHp = 28, HpPerLevel = 5, Mp = 2, Initiative = 80,
                Spells =
                {
                    new SpellDef { Id = "esporos", Name = "Esporos", Element = Element.Water, ApCost = 4, MinRange = 2, MaxRange = 5, Min = 6, Max = 9, MaxCastsPerTurn = 1 },
                    new SpellDef { Id = "cabecada", Name = "Cabeçada", Element = Element.Earth, ApCost = 2, Min = 3, Max = 5, MaxCastsPerTurn = 1 },
                },
                Drops = { new DropDef { ItemId = "esporo", Chance = 65, Max = 2 }, new DropDef { ItemId = "chapeu_cogumelo", Chance = 4 } },
            },
            // ---- Floresta
            new MonsterDef
            {
                Id = "javali", Name = "Javali", BaseHp = 44, HpPerLevel = 7, Mp = 4, Initiative = 90,
                Spells = { new SpellDef { Id = "investida", Name = "Investida", Element = Element.Earth, ApCost = 4, Min = 8, Max = 11, Push = 1 } },
                Drops = { new DropDef { ItemId = "couro", Chance = 65, Max = 2 }, new DropDef { ItemId = "presa_javali", Chance = 30 }, new DropDef { ItemId = "cinto_couro", Chance = 4 } },
            },
            new MonsterDef
            {
                Id = "lobo", Name = "Lobo Cinzento", BaseHp = 38, HpPerLevel = 6, Mp = 5, Initiative = 150,
                Spells = { new SpellDef { Id = "mordida", Name = "Mordida", Element = Element.Fire, ApCost = 3, Min = 7, Max = 10 } },
                Drops = { new DropDef { ItemId = "pelo_lobo", Chance = 60, Max = 2 }, new DropDef { ItemId = "capa_lobo", Chance = 3 } },
            },
            // ---- Pântano
            new MonsterDef
            {
                Id = "sapo", Name = "Sapo Venenoso", BaseHp = 32, HpPerLevel = 5, Mp = 3, Initiative = 100,
                Spells =
                {
                    new SpellDef { Id = "lingua", Name = "Língua Ácida", Element = Element.Water, ApCost = 4, MinRange = 1, MaxRange = 4, Min = 4, Max = 6, PoisonDamage = 4, PoisonTurns = 2, MaxCastsPerTurn = 1 },
                    new SpellDef { Id = "salto_sapo", Name = "Pancada", Element = Element.Earth, ApCost = 2, Min = 3, Max = 5, MaxCastsPerTurn = 1 },
                },
                Drops = { new DropDef { ItemId = "gosma", Chance = 65, Max = 2 }, new DropDef { ItemId = "anel_pantano", Chance = 4 } },
            },
            new MonsterDef
            {
                Id = "morcego", Name = "Morcego", BaseHp = 26, HpPerLevel = 4, Mp = 5, Initiative = 160,
                Spells = { new SpellDef { Id = "sugar", Name = "Sugar", Element = Element.Fire, ApCost = 3, Min = 5, Max = 7, LifeSteal = 50 } },
                Drops = { new DropDef { ItemId = "asa_morcego", Chance = 60, Max = 2 }, new DropDef { ItemId = "amuleto_noite", Chance = 3 } },
            },
            // ---- Ruínas
            new MonsterDef
            {
                Id = "esqueleto", Name = "Esqueleto Arqueiro", BaseHp = 36, HpPerLevel = 5, Mp = 3, Initiative = 110,
                Spells = { new SpellDef { Id = "flecha_osso", Name = "Flecha de Osso", Element = Element.Air, ApCost = 3, MinRange = 2, MaxRange = 6, Min = 6, Max = 9 } },
                Drops = { new DropDef { ItemId = "osso", Chance = 70, Max = 2 }, new DropDef { ItemId = "anel_osso", Chance = 4 } },
            },
            new MonsterDef
            {
                Id = "golem", Name = "Golem de Pedra", BaseHp = 70, HpPerLevel = 9, Mp = 2, Initiative = 40,
                Spells = { new SpellDef { Id = "esmagar", Name = "Esmagar", Element = Element.Earth, ApCost = 5, Min = 12, Max = 16, Push = 1, MaxCastsPerTurn = 1 } },
                Drops = { new DropDef { ItemId = "fragmento", Chance = 70, Max = 2 }, new DropDef { ItemId = "elmo_pedra", Chance = 4 } },
            },
            // ---- Chefe
            new MonsterDef
            {
                Id = "rei_lanudo", Name = "Rei Lanudo", BaseHp = 150, HpPerLevel = 12, Ap = 7, Mp = 3, Initiative = 120, IsBoss = true,
                Spells =
                {
                    new SpellDef { Id = "chifrada_real", Name = "Chifrada Real", Element = Element.Earth, ApCost = 4, Min = 11, Max = 15, Push = 2, MaxCastsPerTurn = 1 },
                    new SpellDef { Id = "tremor", Name = "Tremor", Element = Element.Earth, Target = SpellTarget.Self, ApCost = 3, MinRange = 0, MaxRange = 0, NeedsSight = false, Area = 2, ExcludeCaster = true, Min = 6, Max = 9, Cooldown = 2, MaxCastsPerTurn = 1 },
                },
                Drops = { new DropDef { ItemId = "coroa_la", Chance = 100 }, new DropDef { ItemId = "amuleto_rei", Chance = 35 }, new DropDef { ItemId = "la", Chance = 100, Min = 3, Max = 5 } },
            },
        };

        public static MonsterDef MonsterById(string id) => Monsters.Find(m => m.Id == id) ?? Monsters[0];
    }
}
