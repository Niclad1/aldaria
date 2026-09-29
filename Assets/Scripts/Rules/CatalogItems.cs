using System.Collections.Generic;

namespace Aldaria.Rules
{
    public static partial class Catalog
    {
        static ItemDef Gear(string id, string name, ItemSlot slot, int level, int rarity, int price, Stats stats, string desc) =>
            new ItemDef { Id = id, Name = name, Kind = ItemKind.Equipment, Slot = slot, Level = level, Rarity = rarity, Price = price, Stats = stats, Description = desc };

        static ItemDef Resource(string id, string name, int price, string desc) =>
            new ItemDef { Id = id, Name = name, Kind = ItemKind.Resource, Price = price, Description = desc };

        static ItemDef Potion(string id, string name, int heal, int price, string desc) =>
            new ItemDef { Id = id, Name = name, Kind = ItemKind.Consumable, Heal = heal, Price = price, Description = desc };

        public static readonly List<ItemDef> Items = new List<ItemDef>
        {
            // ---- armas iniciais
            Gear("espada_treino", "Espada de Treino", ItemSlot.Weapon, 1, 0, 30, new Stats { Power = 5 }, "Cega, mas honesta."),
            Gear("arco_curto", "Arco Curto", ItemSlot.Weapon, 1, 0, 30, new Stats { Power = 5 }, "Leve e fácil de puxar."),
            Gear("cajado_galho", "Cajado de Galho", ItemSlot.Weapon, 1, 0, 30, new Stats { Power = 5 }, "Ainda tem umas folhas."),
            Gear("varinha_aprendiz", "Varinha de Aprendiz", ItemSlot.Weapon, 1, 0, 30, new Stats { Power = 5 }, "Solta faíscas quando espirra."),
            Gear("adagas_gemeas", "Adagas Gêmeas", ItemSlot.Weapon, 1, 0, 30, new Stats { Power = 5 }, "Uma para cada mão."),
            Gear("faixas_treino", "Faixas de Treino", ItemSlot.Weapon, 1, 0, 30, new Stats { Power = 5 }, "Protegem os punhos. Mais ou menos."),
            // ---- armas melhores
            Gear("lamina_javali", "Lâmina de Presa", ItemSlot.Weapon, 8, 1, 220, new Stats { Power = 15, Damage = 2 }, "Feita com presas de javali."),
            Gear("cajado_raiz", "Cajado de Raiz Antiga", ItemSlot.Weapon, 12, 2, 480, new Stats { Power = 22, Hp = 15 }, "Pulsa com a seiva da floresta."),

            // ---- chapéus
            Gear("gorro_la", "Gorro de Lã", ItemSlot.Hat, 2, 0, 40, new Stats { Hp = 10 }, "Quentinho e cheira a ovelha."),
            Gear("chapeu_cogumelo", "Chapéu de Cogumelo", ItemSlot.Hat, 5, 1, 110, new Stats { Hp = 15, Power = 5 }, "Não coma."),
            Gear("elmo_pedra", "Elmo de Pedra", ItemSlot.Hat, 14, 2, 520, new Stats { Hp = 45, Power = 5 }, "Pesa uma tonelada. Literalmente quase."),
            // ---- capas
            Gear("capa_penas", "Capa de Penas", ItemSlot.Cloak, 3, 1, 90, new Stats { Hp = 8, Initiative = 30 }, "Faz cócegas no pescoço."),
            Gear("capa_lobo", "Capa do Lobo", ItemSlot.Cloak, 10, 2, 380, new Stats { Hp = 20, Power = 10, Initiative = 20 }, "Uiva sozinha em noites de lua."),
            // ---- amuletos
            Gear("amuleto_la", "Amuleto de Lã Trançada", ItemSlot.Amulet, 4, 0, 80, new Stats { Hp = 15 }, "Presente das tecelãs da vila."),
            Gear("amuleto_noite", "Amuleto da Noite", ItemSlot.Amulet, 9, 2, 360, new Stats { Power = 12, Initiative = 25 }, "Brilha no escuro."),
            Gear("amuleto_rei", "Amuleto do Rei Lanudo", ItemSlot.Amulet, 12, 3, 1500, new Stats { Ap = 1, Hp = 20 }, "Dá um PA a mais. Lendário!"),
            // ---- anéis
            Gear("anel_cobre", "Anel de Cobre", ItemSlot.Ring, 3, 0, 70, new Stats { Power = 6 }, "Deixa o dedo verde."),
            Gear("anel_pantano", "Anel do Pântano", ItemSlot.Ring, 8, 1, 260, new Stats { Damage = 3, Hp = 10 }, "Úmido. Sempre úmido."),
            Gear("anel_osso", "Anel de Osso", ItemSlot.Ring, 11, 2, 420, new Stats { Power = 14, Damage = 1 }, "Talhado de um osso muito antigo."),
            // ---- cintos
            Gear("cinto_couro", "Cinto de Couro", ItemSlot.Belt, 6, 1, 150, new Stats { Hp = 14, Power = 4 }, "Firme e resistente."),
            // ---- botas
            Gear("botas_simples", "Botas Simples", ItemSlot.Boots, 2, 0, 50, new Stats { Hp = 6, Initiative = 15 }, "Melhor que andar descalço."),
            Gear("botas_viajante", "Botas do Viajante", ItemSlot.Boots, 10, 2, 900, new Stats { Mp = 1, Hp = 10 }, "Um PM a mais. Levam você longe."),

            // ---- consumíveis
            Potion("pao", "Pão de Aldaria", 20, 8, "Recém-saído do forno da vila."),
            Potion("pocao_pequena", "Poção Pequena", 35, 20, "Tem gosto de morango."),
            Potion("pocao_media", "Poção Média", 90, 55, "Tem gosto de morango forte."),

            // ---- recursos (drops)
            Resource("la", "Lã de Lanudo", 8, "Macia e cheia de nós."),
            Resource("pena", "Pena de Pipio", 8, "Amarelinha."),
            Resource("esporo", "Esporo Bravo", 10, "Não respire perto."),
            Resource("couro", "Couro de Javali", 16, "Grosso e resistente."),
            Resource("presa_javali", "Presa de Javali", 24, "Afiada como uma adaga."),
            Resource("pelo_lobo", "Pelo de Lobo", 18, "Cinzento e macio."),
            Resource("gosma", "Gosma de Sapo", 16, "Verde, grudenta, nojenta."),
            Resource("asa_morcego", "Asa de Morcego", 18, "Fininha como papel."),
            Resource("osso", "Osso Antigo", 22, "Estala quando você aperta."),
            Resource("fragmento", "Fragmento de Golem", 26, "Ainda vibra um pouco."),
            Resource("coroa_la", "Coroa de Lã", 200, "Prova de que você derrotou o Rei Lanudo."),
        };

        public static ItemDef ItemById(string id) => string.IsNullOrEmpty(id) ? null : Items.Find(i => i.Id == id);
    }
}
