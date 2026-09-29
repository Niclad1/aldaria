// Testes das regras: gera centenas de mapas e simula milhares de lutas com todas as classes,
// conferindo que nada trava, ninguém fica em célula inválida e que missões/inventário funcionam.
using System;
using System.Collections.Generic;
using System.Linq;
using Aldaria.Rules;

static class Program
{
    static void Check(bool ok, string msg) { if (!ok) throw new Exception(msg); }

    static int Main()
    {
        // ---- catálogo consistente
        foreach (var c in Catalog.Classes) Check(Catalog.ItemById(c.StarterWeapon) != null, "arma inicial " + c.Id);
        foreach (var m in Catalog.Monsters) foreach (var d in m.Drops) Check(Catalog.ItemById(d.ItemId) != null, "drop " + d.ItemId);
        foreach (var n in Catalog.Npcs) foreach (var i in n.ShopItems) Check(Catalog.ItemById(i) != null, "loja " + i);
        foreach (var q in Catalog.Quests)
        {
            Check(Catalog.NpcById(q.Giver) != null && Catalog.NpcById(q.TurnIn) != null, "npc da missão " + q.Id);
            if (q.Requires != null) Check(Catalog.QuestById(q.Requires) != null, "req " + q.Id);
            foreach (var it in q.RewardItems) Check(Catalog.ItemById(it.Id) != null, "recompensa " + it.Id);
            foreach (var o in q.Objectives)
            {
                if (o.Kind == ObjectiveKind.Kill) Check(Catalog.Monsters.Any(m => m.Id == o.Target), "alvo " + o.Target);
                if (o.Kind == ObjectiveKind.Collect) Check(Catalog.ItemById(o.Target) != null, "coleta " + o.Target);
                if (o.Kind == ObjectiveKind.Talk) Check(Catalog.NpcById(o.Target) != null, "fala " + o.Target);
            }
        }
        Check(Catalog.Items.Select(i => i.Id).Distinct().Count() == Catalog.Items.Count, "ids de item repetidos");

        // ---- mapas
        var regions = new Dictionary<Region, int>();
        for (int mx = -6; mx <= 6; mx++)
        for (int my = -6; my <= 6; my++)
        {
            var m = MapGenerator.Generate(mx, my);
            var m2 = MapGenerator.Generate(mx, my);
            foreach (var c in m.Cells()) Check(m[c] == m2[c], "não determinístico");
            var reach = Pathfinder.Reachable(m.Center, int.MaxValue, m.IsWalkable);
            foreach (var e in m.Exits) Check(reach.ContainsKey(e) && m.IsWalkable(e), $"saída isolada {mx},{my} {e}");
            foreach (var c in m.Cells()) if (m.IsWalkable(c)) Check(reach.ContainsKey(c), "ilha");
            foreach (var npc in Catalog.NpcsAt(mx, my))
            {
                Check(m.IsWalkable(npc.Cell), "npc em célula ruim " + npc.Id);
                Check(npc.Cell.Neighbors().Any(n => m.IsWalkable(n) && reach.ContainsKey(n)), "npc inalcançável " + npc.Id);
            }
            regions[m.Region] = regions.TryGetValue(m.Region, out var k) ? k + 1 : 1;
            Check(m.Exits.Count == 4 && m.Exits.TrueForAll(e => MapGenerator.SideOf(e).HasValue), "saídas");
            int count = m.Cells().Count();
            Check(count > 450, "mapa pequeno demais: " + count);
        }
        Console.WriteLine("regiões: " + string.Join(", ", regions.Select(kv => $"{kv.Key}={kv.Value}")) + $", células por mapa: {MapGenerator.Generate(1, 0).Cells().Count()}");
        Print(MapGenerator.Generate(0, 0));
                Print(MapGenerator.Generate(-3, 0));

        // ---- efeitos novos
        {
            var map = new GridMap(9, 9);
            var fight = new Fight(map, 1);
            var sombra = Fighter.FromProfile(1, PlayerProfile.Create("S", "assassino"));
            var alvo = Fighter.FromMonster(2, Catalog.MonsterById("golem"), 1);
            fight.AddFighter(sombra, new Cell(4, 4));
            fight.AddFighter(alvo, new Cell(5, 4));
            fight.Start();
            if (fight.Current != sombra) { fight.EndTurn(); }
            Check(fight.Current == sombra, "sombra joga");
            sombra.Hp = 10;
            Check(fight.TryCast(sombra, sombra.Spells.First(s => s.Id == "vampiro"), alvo.Cell), "vampiro");
            Check(sombra.Hp > 10, "roubo de vida curou");
            sombra.Ap = 6;
            Check(fight.TryCast(sombra, sombra.Spells.First(s => s.Id == "adaga"), alvo.Cell), "adaga");
            Check(alvo.Buffs.Any(b => b.Stat == BuffStat.Poison), "veneno aplicado");
            int hp = alvo.Hp;
            fight.EndTurn();
            Check(alvo.Hp == hp - 5 || !alvo.IsAlive, $"veneno tirou 5 ({hp} -> {alvo.Hp})");

            var fight2 = new Fight(map, 2);
            var mago = Fighter.FromProfile(1, PlayerProfile.Create("M", "mago"));
            var perto = Fighter.FromMonster(2, Catalog.MonsterById("golem"), 1);
            var longe = Fighter.FromMonster(3, Catalog.MonsterById("golem"), 1);
            fight2.AddFighter(mago, new Cell(4, 4));
            fight2.AddFighter(perto, new Cell(5, 5));
            fight2.AddFighter(longe, new Cell(8, 4));
            mago.Initiative = 999;
            fight2.Start();
            Check(fight2.TryCast(mago, mago.Spells.First(s => s.Id == "nova"), mago.Cell), "nova gélida");
            Check(mago.Hp == mago.MaxHp, "nova não acerta o mago");
            Check(perto.Hp < perto.MaxHp && longe.Hp == longe.MaxHp, "nova acerta só quem está perto");
            int ap = mago.Ap;
            Check(fight2.TryCast(mago, mago.Spells.First(s => s.Id == "foco"), mago.Cell) && mago.Ap == ap - 1 + 3, "pacto dá PA na hora");
        }

        // ---- inventário, equipamento, loja e missões
        {
            var p = PlayerProfile.Create("Teste", "guerreiro");
            Check(p.IsEquipped("espada_treino") && p.CountItem("pocao_pequena") == 3, "kit inicial");
            int baseHp = p.MaxHp;
            p.AddItem("gorro_la", 1);
            Check(p.Equip("gorro_la") != null, "gorro exige nível 2");
            p.GainXp(100);
            Check(p.Equip("gorro_la") == null && p.MaxHp == Progression.MaxHp(p.Class, p.Level) + 10, "gorro dá PV");
            Check(p.Sell("gorro_la") != null, "não vende equipado único");
            p.Gold = 100;
            Check(p.Buy("pocao_media") == null && p.Gold == 45, "compra");
            p.Hp = 1;
            Check(p.Use("pocao_media") == null && p.Hp == 91 || p.Hp == p.MaxHp, "poção");

            var q = Catalog.QuestById("q_la");
            Check(QuestLog.StatusOf(p, q) == QuestStatus.Available, "disponível");
            Check(QuestLog.Accept(p, q), "aceitar");
            QuestLog.OnKill(p, "lanudo"); QuestLog.OnKill(p, "lanudo"); QuestLog.OnKill(p, "pipio");
            Check(QuestLog.StatusOf(p, q) == QuestStatus.Active, "ainda ativa");
            QuestLog.OnKill(p, "lanudo");
            Check(QuestLog.StatusOf(p, q) == QuestStatus.ReadyToTurnIn, "pronta");
            int gold = p.Gold;
            Check(QuestLog.TurnIn(p, q) >= 0 && p.Gold == gold + 25 && p.CountItem("gorro_la") == 2, "entregou");
            Check(QuestLog.StatusOf(p, Catalog.QuestById("q_mensagem")) == QuestStatus.Available, "cadeia liberada");

            var qp = Catalog.QuestById("q_penas");
            QuestLog.Accept(p, qp);
            p.AddItem("pena", 5);
            Check(QuestLog.StatusOf(p, qp) == QuestStatus.ReadyToTurnIn, "coleta pronta");
            QuestLog.TurnIn(p, qp);
            Check(p.CountItem("pena") == 0 && p.CountItem("capa_penas") == 1, "coleta consumida");

            // salvar/carregar (mesmo formato que o JsonUtility usa: campos públicos)
            var json = System.Text.Json.JsonSerializer.Serialize(p, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
            Check(json.Contains("capa_penas") && json.Contains("q_la"), "serialização");
        }

        // ---- lutas IA x IA com todas as classes e regiões
        var wins = new Dictionary<string, int>(); var rounds = new List<int>();
        int fights = 0, drops = 0;
        foreach (var cls in Catalog.Classes)
        for (int seed = 0; seed < 250; seed++)
        {
            var rng = new Random(seed);
            var mx = seed % 9 - 4; var my = seed % 7 - 3;
            var map = MapGenerator.Generate(mx, my);
            var fight = new Fight(map, seed);
            var prof = PlayerProfile.Create("P", cls.Id);
            prof.GainXp(seed * 7);
            var p = Fighter.FromProfile(1, prof);
            p.IsAI = true;
            var walk = map.Cells().Where(map.IsWalkable).ToList();
            var pc = walk[rng.Next(walk.Count)];
            fight.AddFighter(p, pc);
            var pool = Catalog.MonsterPool(map.Region);
            if (pool.Length == 0) pool = Catalog.MonsterPool(Region.Meadow);
            int n = 1 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                var def = Catalog.MonsterById(seed % 23 == 0 && i == 0 ? "rei_lanudo" : pool[rng.Next(pool.Length)]);
                var free = walk.Where(c => fight.FighterAt(c) == null && c.DistanceTo(pc) >= 5).ToList();
                fight.AddFighter(Fighter.FromMonster(10 + i, def, Math.Max(1, prof.Level - 1 + rng.Next(2))), free[rng.Next(free.Count)]);
            }
            fight.Start();
            int actions = 0;
            while (fight.Phase == FightPhase.Fighting)
            {
                Check(++actions < 5000 && fight.Round <= 80, $"luta travada seed {seed} {cls.Id} round {fight.Round}");
                var cur = fight.Current;
                var a = PlayerAI(fight, cur);
                bool ok = a.Kind switch
                {
                    AiActionKind.Move => fight.TryMove(cur, a.Target),
                    AiActionKind.Cast => fight.TryCast(cur, a.Spell, a.Target),
                    _ => false,
                };
                if (!ok) fight.EndTurn();
                fight.Events.Clear();
                foreach (var f in fight.Fighters) Check(f.Hp >= 0 && f.Hp <= f.MaxHp && f.Ap >= 0 && f.Mp >= 0, "estado inválido");
                var cells = fight.Fighters.Where(f => f.IsAlive).Select(f => f.Cell).ToList();
                Check(cells.Distinct().Count() == cells.Count, "dois lutadores na mesma célula");
                foreach (var f in fight.Fighters.Where(f => f.IsAlive)) Check(map.IsWalkable(f.Cell), "lutador em célula inválida");
                if (fight.Phase == FightPhase.Fighting) Check(fight.Current.IsAlive, "vez de alguém morto");
            }
            fights++;
            rounds.Add(fight.Round);
            if (fight.Winner == Team.Players) drops += Progression.RollDrops(fight.Fighters.Where(f => f.Team == Team.Monsters), rng).Sum(d => d.Count);
            string key = cls.Id + ":" + fight.Winner;
            wins[key] = wins.TryGetValue(key, out var w) ? w + 1 : 1;
        }
        Console.WriteLine($"{fights} lutas ok, rodadas médias {rounds.Average():0.0}, máx {rounds.Max()}, {drops} itens dropados");
        foreach (var cls in Catalog.Classes)
        {
            wins.TryGetValue(cls.Id + ":Players", out var pw);
            Console.WriteLine($"  {cls.Id,-10} vitórias {pw * 100 / 250}%");
        }
        return 0;
    }

    static AiAction PlayerAI(Fight fight, Fighter me)
    {
        if (me.Team == Team.Players)
        {
            foreach (var s in me.Spells)
            {
                if (s.Effect == SpellEffect.Heal && me.Hp < me.MaxHp / 2 && fight.CastProblem(me, s, me.Cell) == null)
                    return new AiAction { Kind = AiActionKind.Cast, Spell = s, Target = me.Cell };
                if (s.Effect == SpellEffect.Buff && fight.CastProblem(me, s, me.Cell) == null)
                    return new AiAction { Kind = AiActionKind.Cast, Spell = s, Target = me.Cell };
            }
            foreach (var s in me.Spells.Where(s => s.Target == SpellTarget.AnyCell))
                foreach (var e in fight.Fighters.Where(f => f.IsAlive && f.Team != me.Team))
                    if (fight.CastProblem(me, s, e.Cell) == null && me.Cell.DistanceTo(e.Cell) > s.Area)
                        return new AiAction { Kind = AiActionKind.Cast, Spell = s, Target = e.Cell };
        }
        return MonsterAI.Decide(fight, me);
    }

    static void Print(GridMap m)
    {
        Console.WriteLine($"{m.Name} [{m.MapX},{m.MapY}] {m.Region}");
        var npcs = Catalog.NpcsAt(m.MapX, m.MapY);
        for (int y = 0; y < m.Height; y++)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = 0; x < m.Width; x++)
            {
                var c = new Cell(x, y);
                if (m[c] == Tile.Void) { sb.Append(' '); continue; }
                if (npcs.Any(n => n.Cell == c)) { sb.Append('@'); continue; }
                sb.Append(m[c] switch { Tile.Grass => '.', Tile.Flowers => '*', Tile.Path => '=', Tile.Water => '~', Tile.Tree => 'T', Tile.Rock => 'o', Tile.House => 'H', Tile.Fence => '#', Tile.Well => 'O', Tile.Pillar => 'I', _ => 'b' });
            }
            Console.WriteLine(sb);
        }
    }
}
