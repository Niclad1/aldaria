using System.Collections.Generic;

namespace Aldaria.Rules
{
    /// <summary>Todas as classes, feitiços e monstros do jogo. Para balancear, é só mexer aqui.</summary>
    public static class Catalog
    {
        public static readonly List<ClassDef> Classes = new List<ClassDef>
        {
            new ClassDef
            {
                Id = "guardiao",
                Name = "Guardião",
                Role = "Corpo a corpo",
                Description = "Guerreiro de armadura que salta no meio da briga e resolve tudo na espada.",
                BaseHp = 62, HpPerLevel = 7, Ap = 6, Mp = 3, Initiative = 110,
                Spells =
                {
                    new SpellDef { Id = "golpe", Name = "Golpe de Espada", Element = Element.Earth, ApCost = 4, MinRange = 1, MaxRange = 1, Min = 13, Max = 17, MaxCastsPerTurn = 2,
                        Description = "Um corte pesado em quem estiver colado em você." },
                    new SpellDef { Id = "pancada", Name = "Pancada", Element = Element.Earth, ApCost = 3, MinRange = 1, MaxRange = 1, Min = 6, Max = 9, Push = 2, MaxCastsPerTurn = 1,
                        Description = "Empurra o alvo 2 células. Se ele bater em algo, sofre dano extra." },
                    new SpellDef { Id = "salto", Name = "Salto", Element = Element.Air, Effect = SpellEffect.Teleport, Target = SpellTarget.EmptyCell, ApCost = 3, MinRange = 2, MaxRange = 4, NeedsSight = false, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Salta até uma célula livre. Não precisa de linha de visão." },
                    new SpellDef { Id = "furia", Name = "Fúria", Element = Element.Fire, Effect = SpellEffect.Buff, Target = SpellTarget.Self, ApCost = 2, MinRange = 0, MaxRange = 0, NeedsSight = false, BuffStat = BuffStat.Damage, Min = 6, Max = 6, BuffTurns = 2, Cooldown = 4, MaxCastsPerTurn = 1,
                        Description = "+6 de dano em todos os feitiços neste turno e no próximo." },
                }
            },
            new ClassDef
            {
                Id = "sentinela",
                Name = "Sentinela",
                Role = "Distância",
                Description = "Arqueira paciente. Fica longe, controla a distância e castiga de longe.",
                BaseHp = 50, HpPerLevel = 5, Ap = 6, Mp = 3, Initiative = 130,
                Spells =
                {
                    new SpellDef { Id = "flecha", Name = "Flecha Mágica", Element = Element.Air, ApCost = 3, MinRange = 2, MaxRange = 7, Min = 8, Max = 11, MaxCastsPerTurn = 2,
                        Description = "Flecha certeira de longo alcance." },
                    new SpellDef { Id = "explosiva", Name = "Flecha Explosiva", Element = Element.Fire, Target = SpellTarget.AnyCell, ApCost = 4, MinRange = 3, MaxRange = 6, Area = 1, Min = 7, Max = 10, Cooldown = 1, MaxCastsPerTurn = 1,
                        Description = "Explode numa área em cruz. Cuidado para não acertar aliados!" },
                    new SpellDef { Id = "recuo", Name = "Flecha de Recuo", Element = Element.Air, ApCost = 2, MinRange = 1, MaxRange = 3, Min = 3, Max = 5, Push = 2, MaxCastsPerTurn = 1,
                        Description = "Empurra o alvo para longe. Ótima para escapar do corpo a corpo." },
                    new SpellDef { Id = "tiro", Name = "Tiro Perfurante", Element = Element.Water, ApCost = 4, MinRange = 1, MaxRange = 8, InLineOnly = true, NeedsSight = false, Min = 9, Max = 12, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Só em linha reta, mas atravessa obstáculos." },
                }
            },
            new ClassDef
            {
                Id = "druida",
                Name = "Druida",
                Role = "Suporte",
                Description = "Guardiã da floresta. Cura, prende os inimigos no lugar e aguenta a luta longa.",
                BaseHp = 56, HpPerLevel = 6, Ap = 6, Mp = 3, Initiative = 100,
                Spells =
                {
                    new SpellDef { Id = "espinhos", Name = "Espinhos", Element = Element.Earth, ApCost = 3, MinRange = 1, MaxRange = 5, Min = 7, Max = 10, MaxCastsPerTurn = 2,
                        Description = "Espinhos brotam do chão sob o alvo." },
                    new SpellDef { Id = "seiva", Name = "Seiva", Element = Element.Water, Effect = SpellEffect.Heal, ApCost = 3, MinRange = 0, MaxRange = 4, Min = 12, Max = 16, Cooldown = 1, MaxCastsPerTurn = 1,
                        Description = "Cura você ou um aliado." },
                    new SpellDef { Id = "raizes", Name = "Raízes", Element = Element.Earth, ApCost = 3, MinRange = 1, MaxRange = 5, Min = 3, Max = 5, MpSteal = 2, Cooldown = 1, MaxCastsPerTurn = 1,
                        Description = "Prende o alvo: ele perde 2 PM no próximo turno." },
                    new SpellDef { Id = "semente", Name = "Semente Explosiva", Element = Element.Water, Target = SpellTarget.AnyCell, ApCost = 4, MinRange = 2, MaxRange = 5, Area = 1, Min = 6, Max = 9, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Uma semente que estoura numa área em cruz." },
                }
            },
        };

        public static readonly List<MonsterDef> Monsters = new List<MonsterDef>
        {
            new MonsterDef
            {
                Id = "lanudo", Name = "Lanudo", BaseHp = 34, HpPerLevel = 6, Ap = 6, Mp = 3, Initiative = 60,
                Spells =
                {
                    new SpellDef { Id = "chifrada", Name = "Chifrada", Element = Element.Earth, ApCost = 3, MinRange = 1, MaxRange = 1, Min = 5, Max = 8, MaxCastsPerTurn = 2 },
                }
            },
            new MonsterDef
            {
                Id = "pipio", Name = "Pipio", BaseHp = 22, HpPerLevel = 4, Ap = 6, Mp = 4, Initiative = 140,
                Spells =
                {
                    new SpellDef { Id = "bicada", Name = "Bicada", Element = Element.Air, ApCost = 3, MinRange = 1, MaxRange = 3, Min = 4, Max = 6, MaxCastsPerTurn = 2 },
                }
            },
            new MonsterDef
            {
                Id = "cogumelo", Name = "Cogumelo Bravo", BaseHp = 28, HpPerLevel = 5, Ap = 6, Mp = 2, Initiative = 80,
                Spells =
                {
                    new SpellDef { Id = "esporos", Name = "Esporos", Element = Element.Water, ApCost = 4, MinRange = 2, MaxRange = 5, Min = 6, Max = 9, MaxCastsPerTurn = 1 },
                    new SpellDef { Id = "cabecada", Name = "Cabeçada", Element = Element.Earth, ApCost = 2, MinRange = 1, MaxRange = 1, Min = 3, Max = 5, MaxCastsPerTurn = 1 },
                }
            },
        };

        public static ClassDef ClassById(string id) => Classes.Find(c => c.Id == id) ?? Classes[0];
        public static MonsterDef MonsterById(string id) => Monsters.Find(m => m.Id == id) ?? Monsters[0];
    }
}
