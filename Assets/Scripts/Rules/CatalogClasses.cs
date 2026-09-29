using System.Collections.Generic;

namespace Aldaria.Rules
{
    /// <summary>Classes jogáveis. Para balancear, é só mexer nos números aqui.</summary>
    public static partial class Catalog
    {
        public static readonly List<ClassDef> Classes = new List<ClassDef>
        {
            new ClassDef
            {
                Id = "guerreiro", Name = "Guerreiro", Role = "Corpo a corpo",
                Description = "Armadura pesada, espada e escudo. Salta no meio da briga e aguenta o tranco.",
                BaseHp = 62, HpPerLevel = 7, Initiative = 110, StarterWeapon = "espada_treino",
                Spells =
                {
                    new SpellDef { Id = "golpe", Name = "Golpe de Espada", Element = Element.Earth, ApCost = 4, MinRange = 1, MaxRange = 1, Min = 13, Max = 17,
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
                Id = "arqueiro", Name = "Arqueiro", Role = "Distância",
                Description = "Paciente e certeiro. Fica longe, controla a distância e castiga de longe.",
                BaseHp = 50, HpPerLevel = 5, Initiative = 130, StarterWeapon = "arco_curto",
                Spells =
                {
                    new SpellDef { Id = "flecha", Name = "Flecha Mágica", Element = Element.Air, ApCost = 3, MinRange = 2, MaxRange = 7, Min = 8, Max = 11,
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
                Id = "mago", Name = "Mago", Role = "Magia sombria",
                Description = "Estudioso das artes proibidas. Frágil, mas transforma o campo de batalha em chamas e gelo negro.",
                BaseHp = 46, HpPerLevel = 5, Initiative = 120, StarterWeapon = "varinha_aprendiz",
                Spells =
                {
                    new SpellDef { Id = "bola_fogo", Name = "Chama Sombria", Element = Element.Fire, ApCost = 4, MinRange = 2, MaxRange = 6, Min = 10, Max = 13,
                        Description = "Uma esfera de fogo negro que explode no alvo." },
                    new SpellDef { Id = "nova", Name = "Nova Gélida", Element = Element.Water, Target = SpellTarget.Self, ApCost = 4, MinRange = 0, MaxRange = 0, NeedsSight = false, Area = 2, ExcludeCaster = true, Min = 7, Max = 10, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Congela tudo ao seu redor (até 2 células), sem atingir você." },
                    new SpellDef { Id = "faisca", Name = "Faísca", Element = Element.Air, ApCost = 2, MinRange = 1, MaxRange = 5, Min = 5, Max = 7, MaxCastsPerTurn = 3,
                        Description = "Barata e rápida: dá para usar até 3 vezes por turno." },
                    new SpellDef { Id = "foco", Name = "Pacto Sombrio", Element = Element.Neutral, Effect = SpellEffect.Buff, Target = SpellTarget.Self, ApCost = 1, MinRange = 0, MaxRange = 0, NeedsSight = false, BuffStat = BuffStat.Ap, Min = 3, Max = 3, BuffTurns = 1, Cooldown = 4, MaxCastsPerTurn = 1,
                        Description = "Ganha +3 PA neste turno." },
                }
            },
            new ClassDef
            {
                Id = "assassino", Name = "Assassino", Role = "Furtivo",
                Description = "Aparece do nada, envenena e some. Mata rápido, mas não aguenta muito tempo exposto.",
                BaseHp = 50, HpPerLevel = 5, Initiative = 150, StarterWeapon = "adagas_gemeas",
                Spells =
                {
                    new SpellDef { Id = "adaga", Name = "Adaga Envenenada", Element = Element.Air, ApCost = 3, MinRange = 1, MaxRange = 1, Min = 6, Max = 8, PoisonDamage = 5, PoisonTurns = 2,
                        Description = "Corte que envenena: o alvo perde 5 PV no início dos próximos 2 turnos." },
                    new SpellDef { Id = "vampiro", Name = "Golpe Vampírico", Element = Element.Fire, ApCost = 4, MinRange = 1, MaxRange = 1, Min = 10, Max = 13, LifeSteal = 50, MaxCastsPerTurn = 1,
                        Description = "Metade do dano causado volta para você como cura." },
                    new SpellDef { Id = "passo", Name = "Passo Sombrio", Element = Element.Air, Effect = SpellEffect.Teleport, Target = SpellTarget.EmptyCell, ApCost = 2, MinRange = 1, MaxRange = 3, NeedsSight = false, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Some e reaparece numa célula livre próxima." },
                    new SpellDef { Id = "arremesso", Name = "Lâmina Arremessada", Element = Element.Earth, ApCost = 3, MinRange = 2, MaxRange = 5, Min = 6, Max = 8,
                        Description = "Uma faca atirada de média distância." },
                }
            },
        };

        public static ClassDef ClassById(string id) => Classes.Find(c => c.Id == id) ?? Classes[0];
    }
}
