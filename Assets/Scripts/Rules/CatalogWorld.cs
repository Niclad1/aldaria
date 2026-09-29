using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    public enum Region
    {
        Village,
        Meadow,
        Forest,
        Swamp,
        Ruins
    }

    /// <summary>Mundo: regiões, NPCs e missões.</summary>
    public static partial class Catalog
    {
        public const int BossMapX = -3;
        public const int BossMapY = 0;

        public static Region RegionOf(int x, int y)
        {
            if (x == 0 && y == 0) return Region.Village;
            if (Math.Abs(x) <= 1 && Math.Abs(y) <= 1) return Region.Meadow;
            if (x >= 2) return Region.Forest;
            if (y >= 2) return Region.Swamp;
            return Region.Ruins;
        }

        public static string RegionName(Region r)
        {
            switch (r)
            {
                case Region.Village: return "Vila";
                case Region.Meadow: return "Pradaria";
                case Region.Forest: return "Floresta";
                case Region.Swamp: return "Pântano";
                default: return "Ruínas";
            }
        }

        /// <summary>Faixa de nível dos monstros: cresce com a distância da vila.</summary>
        public static void LevelRange(int x, int y, out int min, out int max)
        {
            int dist = Math.Abs(x) + Math.Abs(y);
            min = Math.Max(1, dist * 2 - 1);
            max = min + 3;
        }

        public static string[] MonsterPool(Region r)
        {
            switch (r)
            {
                case Region.Meadow: return new[] { "lanudo", "pipio", "cogumelo" };
                case Region.Forest: return new[] { "javali", "lobo", "cogumelo" };
                case Region.Swamp: return new[] { "sapo", "morcego" };
                case Region.Ruins: return new[] { "esqueleto", "golem", "morcego" };
                default: return new string[0];
            }
        }

        // ================================================================== NPCs

        public static readonly List<NpcDef> Npcs = new List<NpcDef>
        {
            new NpcDef { Id = "anciao", Name = "Ancião Borvo", Title = "Líder da vila", MapX = 0, MapY = 0, Cell = new Cell(13, 17),
                Greeting = "Bem-vindo a Aldaria, jovem. Esta vila é pequena, mas o coração dela é grande." },
            new NpcDef { Id = "mercador", Name = "Pipo", Title = "Mercador", MapX = 0, MapY = 0, Cell = new Cell(19, 15),
                Greeting = "Olá, olá! Poções fresquinhas, anéis brilhantes, botas quase novas! E compro tudo o que você trouxer da mata.",
                ShopItems = { "pao", "pocao_pequena", "pocao_media", "gorro_la", "botas_simples", "anel_cobre", "amuleto_la", "botas_viajante" } },
            new NpcDef { Id = "costureira", Name = "Mila", Title = "Costureira", MapX = 0, MapY = 0, Cell = new Cell(15, 12),
                Greeting = "Linha, agulha e paciência. É tudo o que uma boa capa precisa." },
            new NpcDef { Id = "herbalista", Name = "Tula", Title = "Herbalista", MapX = 0, MapY = 0, Cell = new Cell(20, 18),
                Greeting = "Cuidado onde pisa, essas ervas são delicadas. Ao contrário dos cogumelos lá fora..." },
            new NpcDef { Id = "capita", Name = "Capitã Ferra", Title = "Guarda da estrada", MapX = 2, MapY = 0, Cell = new Cell(17, 18),
                Greeting = "Alto lá! Ah, um aventureiro. Ótimo, precisamos de gente com coragem por aqui." },
            new NpcDef { Id = "eremita", Name = "Osvaldo", Title = "Eremita das ruínas", MapX = -2, MapY = 0, Cell = new Cell(15, 18),
                Greeting = "Hmm? Visitas? Faz anos que ninguém vem até as ruínas por vontade própria." },
        };

        public static NpcDef NpcById(string id) => Npcs.Find(n => n.Id == id);
        public static List<NpcDef> NpcsAt(int mapX, int mapY) => Npcs.FindAll(n => n.MapX == mapX && n.MapY == mapY);

        // ================================================================== missões

        public static readonly List<QuestDef> Quests = new List<QuestDef>
        {
            new QuestDef
            {
                Id = "q_la", Name = "Problema de lã", Giver = "anciao", TurnIn = "anciao",
                Offer = "Os Lanudos da pradaria estão comendo nossas plantações! Pode dar um jeito em três deles? Saia da vila por qualquer borda dourada.",
                Progress = "Os Lanudos ainda estão por aí. Três, lembra?",
                Complete = "Muito bem! Tome este gorro. A lã é dos próprios encrenqueiros, ha!",
                Objectives = { new QuestObjective { Kind = ObjectiveKind.Kill, Target = "lanudo", Count = 3, Text = "Derrotar Lanudos" } },
                RewardXp = 60, RewardGold = 25, RewardItems = { new ItemStack { Id = "gorro_la", Count = 1 } },
            },
            new QuestDef
            {
                Id = "q_penas", Name = "Penas para a capa", Giver = "costureira", TurnIn = "costureira",
                Offer = "Estou costurando uma capa de penas, mas os Pipios não colaboram. Me traz 5 Penas de Pipio? A capa fica com você!",
                Progress = "Ainda faltam penas. Os Pipios ficam na pradaria, pertinho da vila.",
                Complete = "Que lindas! Pronto, sua capa está costurada. Vai deixar você mais rápido no início das lutas.",
                Objectives = { new QuestObjective { Kind = ObjectiveKind.Collect, Target = "pena", Count = 5, Text = "Penas de Pipio" } },
                RewardXp = 80, RewardItems = { new ItemStack { Id = "capa_penas", Count = 1 } },
            },
            new QuestDef
            {
                Id = "q_cogumelos", Name = "Cogumelos bravos", Giver = "herbalista", TurnIn = "herbalista", MinLevel = 2,
                Offer = "Os Cogumelos Bravos estão sufocando minhas ervas com esporos. Derrote 4 deles e me traga 3 Esporos para eu estudar um antídoto.",
                Progress = "Quatro cogumelos e três esporos. Nem mais, nem menos. Bom, mais pode.",
                Complete = "Perfeito! Com isso faço um antídoto. Leve estas poções, você vai precisar.",
                Objectives =
                {
                    new QuestObjective { Kind = ObjectiveKind.Kill, Target = "cogumelo", Count = 4, Text = "Derrotar Cogumelos Bravos" },
                    new QuestObjective { Kind = ObjectiveKind.Collect, Target = "esporo", Count = 3, Text = "Esporos Bravos" },
                },
                RewardXp = 120, RewardGold = 30, RewardItems = { new ItemStack { Id = "pocao_pequena", Count = 4 } },
            },
            new QuestDef
            {
                Id = "q_mensagem", Name = "Mensagem para a estrada", Giver = "anciao", TurnIn = "capita", Requires = "q_la",
                Offer = "A Capitã Ferra guarda a estrada da floresta, a leste, no mapa [2,0]. Diga a ela que a vila precisa de ajuda. Ela vai saber o que fazer.",
                Progress = "A Capitã fica no mapa [2,0]. Siga sempre para o leste.",
                Complete = "O Ancião mandou você? Então a coisa está feia mesmo. Obrigada pelo recado.",
                Objectives = { new QuestObjective { Kind = ObjectiveKind.Talk, Target = "capita", Count = 1, Text = "Falar com a Capitã Ferra em [2,0]" } },
                RewardXp = 100, RewardGold = 20,
            },
            new QuestDef
            {
                Id = "q_javalis", Name = "Javalis na estrada", Giver = "capita", TurnIn = "capita", Requires = "q_mensagem", MinLevel = 4,
                Offer = "Os Javalis estão atacando as carroças. Derrote 5 deles e eu te dou a lâmina que mandei forjar com as presas deles.",
                Progress = "Ainda tem Javali demais nessa floresta.",
                Complete = "A estrada está segura de novo! Aqui está sua Lâmina de Presa, você mereceu.",
                Objectives = { new QuestObjective { Kind = ObjectiveKind.Kill, Target = "javali", Count = 5, Text = "Derrotar Javalis" } },
                RewardXp = 300, RewardGold = 60, RewardItems = { new ItemStack { Id = "lamina_javali", Count = 1 } },
            },
            new QuestDef
            {
                Id = "q_pantano", Name = "O pântano sombrio", Giver = "capita", TurnIn = "capita", Requires = "q_javalis", MinLevel = 6,
                Offer = "Meus batedores sumiram no pântano ao sul, perto de [0,2]. Vá até lá e derrote 3 Sapos Venenosos. Cuidado com o veneno!",
                Progress = "O pântano começa ao sul da vila, em [0,2].",
                Complete = "Você voltou inteiro! Pegue este anel, era do meu melhor batedor.",
                Objectives =
                {
                    new QuestObjective { Kind = ObjectiveKind.Visit, Target = "0,2", Count = 1, Text = "Chegar ao pântano [0,2]" },
                    new QuestObjective { Kind = ObjectiveKind.Kill, Target = "sapo", Count = 3, Text = "Derrotar Sapos Venenosos" },
                },
                RewardXp = 400, RewardItems = { new ItemStack { Id = "anel_pantano", Count = 1 } },
            },
            new QuestDef
            {
                Id = "q_ossos", Name = "Ossos inquietos", Giver = "eremita", TurnIn = "eremita", MinLevel = 8,
                Offer = "Os mortos destas ruínas não descansam. Derrote 4 Esqueletos Arqueiros e me traga 3 Fragmentos de Golem para eu selar as tumbas.",
                Progress = "Os esqueletos ficam entre as colunas. Os golens... bom, você vai ouvir quando chegarem perto.",
                Complete = "Silêncio, finalmente. Este anel era de um rei esquecido. Agora é seu.",
                Objectives =
                {
                    new QuestObjective { Kind = ObjectiveKind.Kill, Target = "esqueleto", Count = 4, Text = "Derrotar Esqueletos Arqueiros" },
                    new QuestObjective { Kind = ObjectiveKind.Collect, Target = "fragmento", Count = 3, Text = "Fragmentos de Golem" },
                },
                RewardXp = 600, RewardGold = 80, RewardItems = { new ItemStack { Id = "anel_osso", Count = 1 } },
            },
            new QuestDef
            {
                Id = "q_rei", Name = "O Rei Lanudo", Giver = "anciao", TurnIn = "anciao", Requires = "q_la", MinLevel = 10,
                Offer = "Agora eu sei de onde vêm tantos Lanudos: o Rei Lanudo acordou no trono das ruínas, em [-3,0]. Só alguém forte como você pode detê-lo.",
                Progress = "O Rei Lanudo espera no mapa [-3,0], a oeste. Leve poções!",
                Complete = "Você derrotou o Rei! Aldaria nunca vai esquecer. Estas botas foram do fundador da vila.",
                Objectives = { new QuestObjective { Kind = ObjectiveKind.Kill, Target = "rei_lanudo", Count = 1, Text = "Derrotar o Rei Lanudo em [-3,0]" } },
                RewardXp = 1200, RewardGold = 300, RewardItems = { new ItemStack { Id = "botas_viajante", Count = 1 } },
            },
        };

        public static QuestDef QuestById(string id) => Quests.Find(q => q.Id == id);
    }
}
