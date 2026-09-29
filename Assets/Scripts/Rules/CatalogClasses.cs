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
                Id = "guardiao", Name = "Guardião", Role = "Corpo a corpo",
                Description = "Guerreiro de armadura que salta no meio da briga e resolve tudo na espada.",
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
                Id = "sentinela", Name = "Sentinela", Role = "Distância",
                Description = "Arqueira paciente. Fica longe, controla a distância e castiga de longe.",
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
                Id = "druida", Name = "Druida", Role = "Suporte",
                Description = "Guardiã da floresta. Cura, prende os inimigos no lugar e aguenta a luta longa.",
                BaseHp = 56, HpPerLevel = 6, Initiative = 100, StarterWeapon = "cajado_galho",
                Spells =
                {
                    new SpellDef { Id = "espinhos", Name = "Espinhos", Element = Element.Earth, ApCost = 3, MinRange = 1, MaxRange = 5, Min = 7, Max = 10,
                        Description = "Espinhos brotam do chão sob o alvo." },
                    new SpellDef { Id = "seiva", Name = "Seiva", Element = Element.Water, Effect = SpellEffect.Heal, ApCost = 3, MinRange = 0, MaxRange = 4, Min = 12, Max = 16, Cooldown = 1, MaxCastsPerTurn = 1,
                        Description = "Cura você ou um aliado." },
                    new SpellDef { Id = "raizes", Name = "Raízes", Element = Element.Earth, ApCost = 3, MinRange = 1, MaxRange = 5, Min = 3, Max = 5, MpSteal = 2, Cooldown = 1, MaxCastsPerTurn = 1,
                        Description = "Prende o alvo: ele perde 2 PM no próximo turno." },
                    new SpellDef { Id = "semente", Name = "Semente Explosiva", Element = Element.Water, Target = SpellTarget.AnyCell, ApCost = 4, MinRange = 2, MaxRange = 5, Area = 1, Min = 6, Max = 9, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Uma semente que estoura numa área em cruz." },
                }
            },
            new ClassDef
            {
                Id = "arcanista", Name = "Arcanista", Role = "Magia de área",
                Description = "Estudiosa das runas. Frágil, mas transforma o campo de batalha num mar de fogo e gelo.",
                BaseHp = 46, HpPerLevel = 5, Initiative = 120, StarterWeapon = "varinha_aprendiz",
                Spells =
                {
                    new SpellDef { Id = "bola_fogo", Name = "Bola de Fogo", Element = Element.Fire, ApCost = 4, MinRange = 2, MaxRange = 6, Min = 10, Max = 13,
                        Description = "Uma esfera flamejante que explode no alvo." },
                    new SpellDef { Id = "nova", Name = "Nova Gélida", Element = Element.Water, Target = SpellTarget.Self, ApCost = 4, MinRange = 0, MaxRange = 0, NeedsSight = false, Area = 2, ExcludeCaster = true, Min = 7, Max = 10, Cooldown = 2, MaxCastsPerTurn = 1,
                        Description = "Congela tudo ao seu redor (até 2 células), sem atingir você." },
                    new SpellDef { Id = "faisca", Name = "Faísca", Element = Element.Air, ApCost = 2, MinRange = 1, MaxRange = 5, Min = 5, Max = 7, MaxCastsPerTurn = 3,
                        Description = "Barata e rápida: dá para usar até 3 vezes por turno." },
                    new SpellDef { Id = "foco", Name = "Foco Arcano", Element = Element.Neutral, Effect = SpellEffect.Buff, Target = SpellTarget.Self, ApCost = 1, MinRange = 0, MaxRange = 0, NeedsSight = false, BuffStat = BuffStat.Ap, Min = 3, Max = 3, BuffTurns = 1, Cooldown = 4, MaxCastsPerTurn = 1,
                        Description = "Ganha +3 PA neste turno." },
                }
            },
            new ClassDef
            {
                Id = "sombra", Name = "Sombra", Role = "Assassina",
                Description = "Aparece do nada, envenena e some. Mata rápido, mas não aguenta muito tempo exposta.",
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
            new ClassDef
            {
                Id = "monge", Name = "Monge", Role = "Controle",
                Description = "Mestre das artes marciais. Puxa, empurra e bagunça a posição dos inimigos.",
                BaseHp = 58, HpPerLevel = 6, Initiative = 115, StarterWeapon = "faixas_treino",
                Spells =
                {
                    new SpellDef { Id = "palma", Name = "Palma Explosiva", Element = Element.Air, ApCost = 3, MinRange = 1, MaxRange = 1, Min = 7, Max = 9, Push = 3, MaxCastsPerTurn = 1,
                        Description = "Empurra o alvo 3 células. Bater em obstáculos dói!" },
                    new SpellDef { Id = "atracao", Name = "Atração", Element = Element.Water, ApCost = 2, MinRange = 2, MaxRange = 5, InLineOnly = true, Min = 3, Max = 5, Pull = 3, Cooldown = 1, MaxCastsPerTurn = 1,
                        Description = "Puxa o alvo até 3 células na sua direção (só em linha reta)." },
                    new SpellDef { Id = "sismico", Name = "Punho Sísmico", Element = Element.Earth, Target = SpellTarget.Self, ApCost = 4, MinRange = 0, MaxRange = 0, NeedsSight = false, Area = 1, ExcludeCaster = true, Min = 9, Max = 12, MaxCastsPerTurn = 1,
                        Description = "Soca o chão e atinge todos os vizinhos." },
                    new SpellDef { Id = "meditacao", Name = "Meditação", Element = Element.Neutral, Effect = SpellEffect.Buff, Target = SpellTarget.Self, ApCost = 1, MinRange = 0, MaxRange = 0, NeedsSight = false, BuffStat = BuffStat.Mp, Min = 2, Max = 2, BuffTurns = 2, Cooldown = 4, MaxCastsPerTurn = 1,
                        Description = "+2 PM neste turno e no próximo." },
                }
            },
        };

        public static ClassDef ClassById(string id) => Classes.Find(c => c.Id == id) ?? Classes[0];
    }
}
