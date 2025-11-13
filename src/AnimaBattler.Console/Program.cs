#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// EF layer
using AnimaBattler.Data; // DbContext + Entities (AnimaEntity, SkillEntity, AnimaSkillEntity)

namespace AnimaBattler.Console
{
    enum TeamSide { Player = 0, Enemy = 1 }

    sealed class Unit
    {
        public string Color { get; set; } = "Unknown"; // same as archetype
        public string ColorIcon => ArchetypeIcons.For(Color);

        public string Name { get; set; } = "Unit";
        public TeamSide Side { get; set; }
        public int Slot { get; set; }               // 0..3 front→rear
        public int MaxHP { get; set; } = 60;
        public int HP { get; set; } = 60;
        public int Attack { get; set; } = 12;
        public int Defense { get; set; } = 4;
        public int Speed { get; set; } = 3;
        public int Shield { get; set; } = 0;        // per-round block
        public bool IsAlive => HP > 0;

        public List<Card> Cards { get; } = new();

        public string Icon => Side == TeamSide.Player ? Icons.Player : Icons.Enemy;

        public override string ToString()
            => $"{Icon} {ColorIcon} {Name}(S{Slot}, HP:{HP}/{MaxHP}{(Shield > 0 ? $"+{Shield}" : "")}, Atk:{Attack}, Def:{Defense}, Spd:{Speed})";
    }

    static class Icons
    {
        public const string Player = "🐾";
        public const string Enemy = "👿";

        public const string Attack = "💥";
        public const string Shield = "🛡️";
        public const string Heal = "✨";

        public static string ForKind(string kind) => kind switch
        {
            "Attack" => Attack,
            "Shield" => Shield,
            "Heal" => Heal,
            _ => ""
        };
    }

    static class ArchetypeIcons
    {
        public static string For(string? archetype)
        {
            if (string.IsNullOrWhiteSpace(archetype))
                return "❓";

            return archetype.ToLowerInvariant() switch
            {
                "gray" => "🛡️", // Defense
                "red" => "🔥", // Physical DPS
                "green" => "🌿", // Support / Nature
                "blue" => "💧", // Magic / Control
                "yellow" => "⚡", // Speed / Utility
                "purple" => "☠️", // Death / Shadow
                _ => "❓"
            };
        }
    }


    sealed class Card
    {
        public long SkillId { get; set; }
        public string Name { get; set; } = "Card";
        public string Kind { get; set; } = "Attack"; // Attack | Shield | Heal
        public string Description { get; set; } = string.Empty;
        public string Slot { get; set; } = "?";  // A..F
        public int Cost { get; set; } = 1;           // default (no Skill.Cost in DB)
        public Unit Owner { get; set; } = default!;

        // inside class Card
        public Unit? Target { get; set; } // set during player selection

        public override string ToString()
        {
            var desc = string.IsNullOrWhiteSpace(Description) ? "" : $" – {Description}";
            var icon = Icons.ForKind(Kind);
            return $"{icon} {Name} [{Kind}] (E{Cost}){desc} — {Owner.Name}";
        }
    }

    sealed class Team
    {
        public TeamSide Side { get; }
        public Unit[] Slots { get; } = new Unit[4];
        public Team(TeamSide side) => Side = side;

        public IEnumerable<Unit> Alive => Slots.Where(u => u != null && u.IsAlive)!;
        public bool AnyAlive() => Alive.Any();
        public Unit? FrontMostAlive() => Slots.FirstOrDefault(u => u != null && u.IsAlive);
    }

    sealed class BattleContext
    {
        public Team Player { get; }
        public Team Enemy { get; }
        public int Round { get; set; } = 1;
        public Random Rng { get; }

        public BattleContext(Team p, Team e, int seed = 777) { Player = p; Enemy = e; Rng = new Random(seed); }

        public Team TeamOf(TeamSide side) => side == TeamSide.Player ? Player : Enemy;
        public Team OpponentOf(TeamSide side) => side == TeamSide.Player ? Enemy : Player;

        public void Log(string s) => System.Console.WriteLine(s);
    }

    static class Rules
    {
        public static int ComputeDamage(Unit attacker, Unit target)
            => Math.Max(1, attacker.Attack - target.Defense);

        public static void ApplyDamage(Unit target, int dmg, BattleContext ctx)
        {
            int absorbed = Math.Min(target.Shield, dmg);
            target.Shield -= absorbed;
            int hpDmg = dmg - absorbed;
            if (hpDmg > 0) target.HP = Math.Max(0, target.HP - hpDmg);
            if (!target.IsAlive) ctx.Log($"💀 {target.Name} is defeated!");
        }

        public static void ApplyShield(Unit target, int amount)
            => target.Shield += Math.Max(0, amount);

        public static void ApplyHeal(Unit target, int amount)
            => target.HP = Math.Min(target.MaxHP, target.HP + Math.Max(0, amount));
    }

    static class Engine
    {
        public static List<Unit> BuildInitiative(BattleContext ctx)
            => ctx.Player.Alive.Concat(ctx.Enemy.Alive)
                .OrderByDescending(u => u.Speed)
                .ThenBy(u => u.Side)
                .ThenBy(u => u.Slot)
                .ToList();

        public sealed class EnemyIntent
        {
            public Unit Actor { get; set; } = default!;
            public string Kind { get; set; } = "Attack"; // Attack|Shield|Heal
        }

        public static List<EnemyIntent> PickEnemyIntents(BattleContext ctx)
        {
            var intents = new List<EnemyIntent>();
            foreach (var u in ctx.Enemy.Alive)
            {
                var allyFront = ctx.Enemy.FrontMostAlive();
                string kind = "Attack";
                if (allyFront != null && allyFront.HP < allyFront.MaxHP * 0.4) kind = "Heal";
                else if (allyFront != null && allyFront.Shield < 6) kind = "Shield";
                intents.Add(new EnemyIntent { Actor = u, Kind = kind });
            }
            return intents;
        }

        public static void ExecutePlayerCard(BattleContext ctx, Card card)
        {
            var actor = card.Owner;
            if (!actor.IsAlive) return;

            switch (card.Kind)
            {
                case "Attack":
                    {
                        var target = card.Target ?? ctx.OpponentOf(actor.Side).FrontMostAlive();
                        if (target == null) return;
                        int dmg = Rules.ComputeDamage(actor, target);
                        ctx.Log($"{actor.Icon}{Icons.Attack} {actor.Name} uses {card.Name} → {target.Icon}{target.Name} for {dmg}");
                        Rules.ApplyDamage(target, dmg, ctx);
                        break;
                    }
                case "Shield":
                    {
                        var target = card.Target ?? actor;
                        int amount = 8;
                        Rules.ApplyShield(target, amount);
                        ctx.Log($"{actor.Icon}{Icons.Shield} {actor.Name} uses {card.Name} → {target.Icon}{target.Name} +{amount} shield (now {target.Shield})");
                        break;
                    }
                case "Heal":
                    {
                        var target = card.Target ?? actor;
                        int amount = 8;
                        Rules.ApplyHeal(target, amount);
                        ctx.Log($"{actor.Icon}{Icons.Heal} {actor.Name} uses {card.Name} → {target.Icon}{target.Name} +{amount} HP (now {target.HP}/{target.MaxHP})");
                        break;
                    }
            }
        }


        public static void ExecuteEnemyIntent(BattleContext ctx, EnemyIntent intent)
        {
            var actor = intent.Actor;
            if (!actor.IsAlive) return;

            switch (intent.Kind)
            {
                case "Attack":
                    {
                        var target = ctx.OpponentOf(actor.Side).FrontMostAlive();
                        if (target == null) return;
                        int dmg = Rules.ComputeDamage(actor, target);
                        ctx.Log($"{actor.Icon}🗡️ {actor.Name} attacks → {target.Name} for {dmg}");
                        Rules.ApplyDamage(target, dmg, ctx);
                        break;
                    }
                case "Shield":
                    {
                        var target = ctx.TeamOf(actor.Side).FrontMostAlive() ?? actor;
                        int amount = 6;
                        Rules.ApplyShield(target, amount);
                        ctx.Log($"{actor.Icon}🛡️ {actor.Name} shields {target.Name} +{amount} (now {target.Shield})");
                        break;
                    }
                case "Heal":
                    {
                        var target = ctx.TeamOf(actor.Side).FrontMostAlive() ?? actor;
                        int amount = 6;
                        Rules.ApplyHeal(target, amount);
                        ctx.Log($"{actor.Icon}✨ {actor.Name} heals {target.Name} +{amount} (HP {target.HP}/{target.MaxHP})");
                        break;
                    }
            }
        }
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            System.Console.Title = "AnimaBattler - PvE Sandbox";

            // .env first (if you added DotNetEnv earlier)
            try { DotNetEnv.Env.Load(); } catch { /* ignore if not installed */ }

            var conn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=animabattler;Username=postgres;Password=postgres";

            var services = new ServiceCollection()
                .AddDbContext<GameDbContext>(o => o.UseNpgsql(conn))
                .BuildServiceProvider();

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            var rng = new Random(DateTime.UtcNow.Millisecond ^ Environment.ProcessId);

            Team playerTeam;

            // PREVIEW + REROLL LOOP
            while (true)
            {
                // Load a random 4-anima team (no duplicates)
                playerTeam = await LoadPlayerTeamAsync(db, rng, randomize: true);

                // Show preview: player team + each anima's cards
                ShowTeamWithSkills("PLAYER PREVIEW", playerTeam);

                System.Console.Write("\nStart with this team?  (S)tart  (R)eroll  (Q)uit: ");
                var ans = (System.Console.ReadLine() ?? "").Trim().ToLowerInvariant();

                if (ans == "s" || ans == "start" || ans == "y" || ans == "yes")
                    break;
                if (ans == "q" || ans == "quit" || ans == "exit")
                    return; // exit app, user aborted

                // else any other input → reroll again
                System.Console.WriteLine("↻ Re-rolling team...\n");
            }

            var enemyTeam = BuildEnemyTeam();
            var ctx = new BattleContext(playerTeam, enemyTeam, seed: 777);

            await RunBattleAsync(ctx);
        }

        private static void ShowTeamWithSkills(string title, Team team)
        {
            System.Console.WriteLine($"\n=== {title} ===");
            foreach (var u in team.Slots.Where(u => u != null))
            {
                System.Console.WriteLine($"{u.ColorIcon} [{u!.Slot}] {u.Name}  HP {u.HP}/{u.MaxHP}  Atk {u.Attack}  Def {u.Defense}  Spd {u.Speed}");
                if (u.Cards.Count == 0)
                {
                    System.Console.WriteLine("   (no skills)");
                    continue;
                }
                int k = 0;
                foreach (var c in u.Cards)
                {
                    var desc = string.IsNullOrWhiteSpace(c.Description) ? "" : $" – {c.Description}";
                    System.Console.WriteLine($"   ({k++}) {c.Name} [{c.Kind}] (E{c.Cost}){desc}");
                }
            }
            System.Console.WriteLine("=== END PREVIEW ===");
        }

        // -------- Load Player Team (uses AnimaEntity / AnimaSkillEntity / SkillEntity) --------       
        private static async Task<Team> LoadPlayerTeamAsync(GameDbContext db, Random rng, bool randomize)
        {
            var team = new Team(TeamSide.Player);

            // 1) Pick 4 animas
            List<AnimaEntity> animas;
            if (randomize)
            {
                var allIds = await db.Set<AnimaEntity>().Select(a => a.Id).ToListAsync();
                if (allIds.Count == 0)
                    throw new InvalidOperationException("No Animas found in DB.");

                var take = Math.Min(4, allIds.Count);
                var picked = new HashSet<long>();
                while (picked.Count < take)
                    picked.Add(allIds[rng.Next(allIds.Count)]);

                var pickedList = picked.ToList();
                animas = await db.Set<AnimaEntity>()
                    .Where(a => pickedList.Contains(a.Id))
                    .OrderBy(a => a.Id)
                    .ToListAsync();
            }
            else
            {
                animas = await db.Set<AnimaEntity>()
                    .OrderBy(a => a.Id)
                    .Take(4)
                    .ToListAsync();
            }

            if (animas.Count == 0)
                throw new InvalidOperationException("No Animas found in DB.");

            // 2) Preload links + skills for those animas
            var animaIds = animas.Select(a => a.Id).ToList();
            var links = await db.Set<AnimaSkillEntity>()
                .Where(x => animaIds.Contains(x.AnimaId))
                .Include(x => x.Skill)
                .ToListAsync();

            // 3) Build units
            for (int i = 0; i < 4; i++)
            {
                var a = animas.ElementAtOrDefault(i) ?? animas.Last(); // ensure () if you type .Last()

                var u = new Unit
                {
                    Name = a.Name ?? $"Anima#{a.Id}",
                    Color = a.Color.ToString(),
                    Side = TeamSide.Player,
                    Slot = i,
                    MaxHP = 60 + (i == 0 ? 20 : 0),
                    HP = 60 + (i == 0 ? 20 : 0),
                    Attack = 12 + (i == 1 ? 3 : 0),
                    Defense = 4 + (i == 0 ? 4 : 0),
                    Speed = 3 + (i >= 2 ? 1 : 0)
                };

                // Links strictly for THIS anima
                var myLinks = links
                    .Where(l => l.AnimaId == a.Id && l.Skill != null)
                    .ToList();

                // Enforce 1 card per Slot (A..F). Group by textual slot key.
                var perSlotLinks = myLinks
                    .GroupBy(l => (l.Skill!.Slot.ToString() ?? "?").ToUpperInvariant())
                    .Select(g => g.OrderBy(l => l.SkillId).First())
                    .ToList();

                // ✅ map from the LINK (so we can use per-link fields too)
                var cards = perSlotLinks.Select(l => ToCard(u, l)).ToList();

                // Fallbacks if empty
                if (!cards.Any())
                {
                    cards.Add(new Card { Owner = u, Name = "Strike", Kind = "Attack", Cost = 1, Description = "Basic attack to the frontmost enemy." });
                    cards.Add(new Card { Owner = u, Name = "Guard", Kind = "Shield", Cost = 1, Description = "Gain a small shield this round." });
                }

                foreach (var c in cards) c.Owner = u;

                u.Cards.AddRange(cards);
                team.Slots[i] = u;

                // Debug (optional):
                // System.Console.WriteLine($"DEBUG {u.Name} ({a.Id},{a.Color}) → SkillIds: {string.Join(", ", perSlotLinks.Select(l => l.SkillId))}");
            }

            return team;
        }



        // Map SkillEntity → Card
        private static Card ToCard(Unit owner, AnimaSkillEntity link)
        {
            var s = link.Skill ?? throw new InvalidOperationException("AnimaSkill link missing Skill.");

            int cost = ResolveCostFromLink(link) ?? ResolveCostFromSkill(s) ?? 1;

            return new Card
            {
                SkillId = s.Id,
                Name = string.IsNullOrWhiteSpace(s.Name) ? (s.Code ?? $"Skill#{s.Id}") : s.Name!,
                Kind = ClassifyKindByNameOrCode(s.Name, s.Code),
                Cost = Math.Max(0, cost),
                Owner = owner,
                Description = s.Description ?? "",
                Slot = SlotLetterFromSkill(s)
            };
        }


        private static string ClassifyKindByNameOrCode(string? name, string? code)
        {
            string text = ((name ?? "") + " " + (code ?? "")).ToLowerInvariant();
            if (text.Contains("heal") || text.Contains("mend") || text.Contains("restore")) return "Heal";
            if (text.Contains("shield") || text.Contains("guard") || text.Contains("block") || text.Contains("fortify")) return "Shield";
            return "Attack";
        }

        // -------- Enemies (hard-coded) --------
        private static Team BuildEnemyTeam()
        {
            var t = new Team(TeamSide.Enemy);
            t.Slots[0] = new Unit { Name = "Goblin Guard", Side = TeamSide.Enemy, Slot = 0, MaxHP = 70, HP = 70, Attack = 11, Defense = 6, Speed = 3 };
            t.Slots[1] = new Unit { Name = "Raider", Side = TeamSide.Enemy, Slot = 1, MaxHP = 55, HP = 55, Attack = 15, Defense = 3, Speed = 4 };
            t.Slots[2] = new Unit { Name = "Shaman", Side = TeamSide.Enemy, Slot = 2, MaxHP = 45, HP = 45, Attack = 12, Defense = 2, Speed = 5 };
            t.Slots[3] = new Unit { Name = "Archer", Side = TeamSide.Enemy, Slot = 3, MaxHP = 40, HP = 40, Attack = 13, Defense = 1, Speed = 6 };
            return t;
        }

        // -------- Battle loop --------
        private static async Task RunBattleAsync(BattleContext ctx)
        {
            ctx.Log("=== BATTLE START ===");
            PrintLineups(ctx);

            var drawPile = ctx.Player.Slots.Where(u => u != null)
                .SelectMany(u => u!.Cards).ToList();
            var discard = new List<Card>();

            for (int round = 1; round <= 50; round++)
            {
                ctx.Round = round;
                ctx.Log($"\n-- Round {round} --");

                var initiative = Engine.BuildInitiative(ctx);

                // (2) Draw 5 (reshuffle if needed)
                var hand = DrawCards(ctx, drawPile, discard, 5);

                // (2b) Enemy intents
                var enemyIntents = Engine.PickEnemyIntents(ctx);
                ctx.Log("Enemy intents:");
                foreach (var ei in enemyIntents) ctx.Log($"  • {ei.Actor.Name} intends to {ei.Kind}");

                // SHOW: Enemy health → Enemy intent → Player health
                PrintRoundStatusAndIntents(ctx, enemyIntents);

                // (3) Energy
                int energy = 3;
                ctx.Log($"\nYou draw {hand.Count} cards (Energy = {energy}).");
                for (int i = 0; i < hand.Count; i++) ctx.Log($"  [{i}] {hand[i]}");

                // (4) Choose within energy
                var chosen = ChooseCardsFromHand(ctx, hand, energy);

                // (5) Resolve by initiative
                foreach (var actor in initiative)
                {
                    if (!actor.IsAlive) continue;
                    if (!ctx.Player.AnyAlive() || !ctx.Enemy.AnyAlive()) break;

                    if (actor.Side == TeamSide.Player)
                    {
                        var myCards = chosen.Where(c => c.Owner == actor).ToList();
                        foreach (var c in myCards)
                            Engine.ExecutePlayerCard(ctx, c);
                    }
                    else
                    {
                        var intent = enemyIntents.FirstOrDefault(ei => ei.Actor == actor);
                        if (intent != null)
                            Engine.ExecuteEnemyIntent(ctx, intent);
                    }
                }

                // End of round: clear shields
                foreach (var u in ctx.Player.Slots.Concat(ctx.Enemy.Slots).Where(u => u != null))
                    u!.Shield = 0;

                // (6) Discard played
                discard.AddRange(chosen);
                foreach (var c in chosen) hand.Remove(c);

                // (7) Victory checks
                if (!ctx.Enemy.AnyAlive()) { ctx.Log("\n✅ Victory! Player wins."); break; }
                if (!ctx.Player.AnyAlive()) { ctx.Log("\n❌ Defeat! Enemies win."); break; }

                await Task.Delay(50);
            }

            PrintFinal(ctx);
            ctx.Log("=== BATTLE END ===");
        }

        private static List<Card> DrawCards(BattleContext ctx, List<Card> drawPile, List<Card> discard, int n)
        {
            var result = new List<Card>(n);
            while (result.Count < n)
            {
                if (drawPile.Count == 0)
                {
                    if (discard.Count == 0) break;
                    ctx.Log("🔁 Reshuffle discard into draw pile.");
                    drawPile.AddRange(discard);
                    discard.Clear();
                }
                int idx = ctx.Rng.Next(drawPile.Count);
                var card = drawPile[idx];
                drawPile.RemoveAt(idx);
                result.Add(card);
            }
            return result;
        }

        private static List<Card> ChooseCardsFromHand(BattleContext ctx, List<Card> hand, int energy)
        {
            while (true) // loop until user confirms
            {
                var chosen = new List<Card>();
                var used = new HashSet<int>();
                int remaining = energy;

                while (true)
                {
                    // Show hand with selection state + energy
                    System.Console.WriteLine();
                    System.Console.WriteLine($"Energy: {remaining}   (type an index to select, 'done' to finish, 'undo' to remove last, Enter for auto)");
                    for (int i = 0; i < hand.Count; i++)
                    {
                        var tag = used.Contains(i) ? "SELECTED" : "";
                        System.Console.WriteLine($"  [{i}] {hand[i]} {tag}");
                    }

                    System.Console.Write("> ");
                    var line = System.Console.ReadLine();

                    // Auto mode: pick cheapest that fit
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        foreach (var item in hand
                            .Select((c, idx) => (candidate: c, index: idx))
                            .OrderBy(t => t.candidate.Cost))
                        {
                            var idx = item.index;
                            var candidate = item.candidate;

                            if (used.Contains(idx)) continue;
                            if (candidate.Cost <= remaining)
                            {
                                // assign default targets in auto mode
                                if (candidate.Kind == "Attack")
                                    candidate.Target = ctx.Enemy.FrontMostAlive();
                                else if (candidate.Kind == "Shield" || candidate.Kind == "Heal")
                                    candidate.Target = candidate.Owner; // self default

                                chosen.Add(candidate);
                                used.Add(idx);
                                remaining -= candidate.Cost;
                            }
                        }
                        break; // finished selecting
                    }

                    if (line.Equals("done", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (line.Equals("undo", StringComparison.OrdinalIgnoreCase))
                    {
                        if (chosen.Count > 0)
                        {
                            var last = chosen[^1];
                            int undoIdx = hand.IndexOf(last);
                            if (undoIdx >= 0) used.Remove(undoIdx);
                            remaining += last.Cost;
                            chosen.RemoveAt(chosen.Count - 1);
                            System.Console.WriteLine($"↩️  Removed {last.Name}. Energy back to {remaining}.");
                        }
                        else
                        {
                            System.Console.WriteLine("⚠️ Nothing to undo.");
                        }
                        continue;
                    }

                    if (!int.TryParse(line, out int pick) || pick < 0 || pick >= hand.Count)
                    {
                        System.Console.WriteLine("⚠️ Invalid index.");
                        continue;
                    }
                    if (used.Contains(pick))
                    {
                        System.Console.WriteLine("⚠️ You already selected that card.");
                        continue;
                    }

                    var selected = hand[pick];

                    // Energy check
                    if (selected.Cost > remaining)
                    {
                        System.Console.WriteLine($"❌ Not enough energy for [{pick}] {selected.Name} (cost {selected.Cost}). Remaining: {remaining}.");
                        continue;
                    }

                    // Choose target based on kind
                    if (selected.Kind == "Attack")
                    {
                        var target = SelectEnemyTarget(ctx);
                        if (target == null)
                        {
                            System.Console.WriteLine("⚠️ No valid enemy target.");
                            continue;
                        }
                        selected.Target = target;
                    }
                    else if (selected.Kind == "Shield" || selected.Kind == "Heal")
                    {
                        var target = SelectAllyTarget(ctx, selected.Owner);
                        selected.Target = target ?? selected.Owner;
                    }

                    chosen.Add(selected);
                    used.Add(pick);
                    remaining -= selected.Cost;
                    System.Console.WriteLine($"✓ Chose [{pick}] {selected.Name} → {(selected.Target != null ? selected.Target.Name : "(no target)")}  (cost {selected.Cost}). Remaining energy: {remaining}.");

                    if (remaining == 0)
                    {
                        System.Console.WriteLine("⚡ Energy spent.");
                        break;
                    }
                }

                // Summary + confirmation
                System.Console.WriteLine("\nPlanned plays:");
                if (chosen.Count == 0)
                    System.Console.WriteLine("  (none)");
                else
                    foreach (var c in chosen)
                        System.Console.WriteLine($"  • {c.Owner.Name} uses {c.Name} [{c.Kind}] → {(c.Target != null ? c.Target.Name : "(no target)")} (E{c.Cost})");

                System.Console.Write("Execute these plays? (y/N): ");
                var confirm = System.Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(confirm) && confirm.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    return chosen;

                System.Console.WriteLine("✋ Selection canceled. Let’s choose again.");
                // loop and re-pick
            }
        }


        private static void PrintLineups(BattleContext ctx)
        {
            System.Console.WriteLine("Player Team:");
            foreach (var u in ctx.Player.Slots.Where(u => u != null))
                System.Console.WriteLine($"  [{u!.Slot}] {u}");

            System.Console.WriteLine("Enemy Team:");
            foreach (var u in ctx.Enemy.Slots.Where(u => u != null))
                System.Console.WriteLine($"  [{u!.Slot}] {u}");
        }

        private static void PrintFinal(BattleContext ctx)
        {
            System.Console.WriteLine("\nFinal Status:");
            foreach (var u in ctx.Player.Slots.Where(u => u != null))
                System.Console.WriteLine($"  Player [{u!.Slot}] {u!.Name} HP {u.HP}/{u.MaxHP}");
            foreach (var u in ctx.Enemy.Slots.Where(u => u != null))
                System.Console.WriteLine($"  Enemy  [{u!.Slot}] {u!.Name} HP {u.HP}/{u.MaxHP}");
        }

        private static void PrintTeamHealth(string title, IEnumerable<Unit> units)
        {
            System.Console.WriteLine(title);
            foreach (var u in units.Where(u => u != null))
            {
                var sh = u!.Shield > 0 ? $"+{u.Shield}" : "0";
                System.Console.WriteLine($"  [{u.Slot}] {u.Name}  HP {u.HP}/{u.MaxHP}  Shield {sh}");
            }
        }

        private static void PrintRoundStatusAndIntents(BattleContext ctx, List<Engine.EnemyIntent> enemyIntents)
        {
            // 3) Enemy health first
            PrintTeamHealth("Enemy Status:", ctx.Enemy.Slots!);

            // Enemy intents next
            System.Console.WriteLine("Enemy intents:");
            foreach (var ei in enemyIntents)
                System.Console.WriteLine($"  • {ei.Actor.Name} intends to {ei.Kind}");

            // 4) Player health
            PrintTeamHealth("Player Status:", ctx.Player.Slots!);
        }

        private static Unit? SelectEnemyTarget(BattleContext ctx)
        {
            var enemies = ctx.Enemy.Slots.Where(u => u != null && u.IsAlive).ToList();
            if (enemies.Count == 0) return null;

            System.Console.WriteLine("\nChoose enemy target:");
            for (int i = 0; i < enemies.Count; i++)
                System.Console.WriteLine($"  [{i}] {enemies[i]!.Name} (HP {enemies[i]!.HP}/{enemies[i]!.MaxHP})");

            System.Console.Write("Target index: ");
            var input = System.Console.ReadLine();
            if (int.TryParse(input, out int idx) && idx >= 0 && idx < enemies.Count)
                return enemies[idx];

            System.Console.WriteLine("⚠️ Invalid target, defaulting to front-most enemy.");
            return ctx.Enemy.FrontMostAlive();
        }

        private static Unit? SelectAllyTarget(BattleContext ctx, Unit owner)
        {
            var allies = ctx.TeamOf(owner.Side).Slots.Where(u => u != null && u.IsAlive).ToList();
            if (allies.Count == 0) return owner;

            System.Console.WriteLine("\nChoose ally target:");
            for (int i = 0; i < allies.Count; i++)
                System.Console.WriteLine($"  [{i}] {allies[i]!.Name} (HP {allies[i]!.HP}/{allies[i]!.MaxHP})");

            System.Console.Write("Target index (Enter for self): ");
            var input = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return owner;
            if (int.TryParse(input, out int idx) && idx >= 0 && idx < allies.Count)
                return allies[idx];

            System.Console.WriteLine("⚠️ Invalid target, defaulting to self.");
            return owner;
        }

        private static int? ResolveCostFromLink(object link)
        {
            return GetIntLike(link, "Energy", "EnergyCost", "Cost", "APCost", "ManaCost", "CostOverride");
        }

        private static int? ResolveCostFromSkill(object skill)
        {
            return GetIntLike(skill, "Energy", "EnergyCost", "Cost", "APCost", "ManaCost");
        }

        private static int? GetIntLike(object obj, params string[] names)
        {
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n);
                if (p == null) continue;

                var v = p.GetValue(obj);
                if (v == null) continue;

                // Direct numeric types
                if (v is int) return (int)v;
                if (v is short) return (short)v;
                if (v is byte) return (byte)v;
                if (v is long) return (int)(long)v;
                if (v is decimal) return (int)(decimal)v;
                if (v is double) return (int)(double)v;
                if (v is float) return (int)(float)v;

                // Nullable numerics handled by unboxing
                var type = v.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var innerType = Nullable.GetUnderlyingType(type);
                    if (innerType == typeof(int)) return (int)v;
                    if (innerType == typeof(short)) return Convert.ToInt32(v);
                    if (innerType == typeof(byte)) return Convert.ToInt32(v);
                    if (innerType == typeof(long)) return Convert.ToInt32(v);
                    if (innerType == typeof(decimal)) return Convert.ToInt32(v);
                    if (innerType == typeof(double)) return Convert.ToInt32(v);
                    if (innerType == typeof(float)) return Convert.ToInt32(v);
                }

                // Strings (e.g., "2")
                if (v is string s && int.TryParse(s, out var parsed))
                    return parsed;
            }
            return null;
        }


        private static string SlotLetterFromSkill(SkillEntity s)
        {
            var slotText = s.Slot.ToString()?.Trim() ?? "";
            switch (slotText.ToUpperInvariant())
            {
                case "A": case "PARTA": case "SLOTA": return "A";
                case "B": case "PARTB": case "SLOTB": return "B";
                case "C": case "PARTC": case "SLOTC": return "C";
                case "D": case "PARTD": case "SLOTD": return "D";
                case "E": case "PARTE": case "SLOTE": return "E";
                case "F": case "PARTF": case "SLOTF": return "F";
                default:
                    if (slotText.Length == 1 && "ABCDEF".Contains(slotText.ToUpperInvariant()))
                        return slotText.ToUpperInvariant();
                    return "";
            }
        }
    }
}
