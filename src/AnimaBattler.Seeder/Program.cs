#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AnimaBattler.Data;
using AnimaBattler.Seeder;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var skillsCsvArg = args.Length > 0 ? args[0] : "data/skills.csv";
var skillsCsvPath = Path.GetFullPath(skillsCsvArg);

DotNetEnv.Env.Load();
var connString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
if (string.IsNullOrWhiteSpace(connString))
{
    Console.Error.WriteLine("❌ Missing POSTGRES_CONNECTION. Add it to .env or system environment.");
    Environment.Exit(1);
}

using var services = new ServiceCollection()
    .AddLogging(lb =>
    {
        lb.ClearProviders();
        lb.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
        lb.SetMinimumLevel(LogLevel.Information);
    })
    .AddDbContext<GameDbContext>(o =>
        o.UseNpgsql(connString, npg => npg.MigrationsAssembly(typeof(GameDbContext).Assembly.FullName)))
    .BuildServiceProvider();

try
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

    logger.LogInformation("DB: applying migrations…");
    logger.LogInformation("Using connection: {conn}", string.Join(";", connString.Split(';').Select(kv => kv.StartsWith("Password", StringComparison.OrdinalIgnoreCase) ? "Password=****" : kv)));

    await db.Database.MigrateAsync(cts.Token);

    // 1) Archetypes first
    await ArchetypeSeeder.SeedDefaultsAsync(db /*, cts.Token if supported*/);

    // 2) Player skills (CSV)
    if (File.Exists(skillsCsvPath))
    {
        logger.LogInformation("Seeding skills from CSV: {path}", skillsCsvPath);
        await SkillSeeder.SeedFromCsvAsync(db, skillsCsvPath, upsert: true /*, cts.Token*/);
    }
    else
    {
        logger.LogWarning("Skills CSV not found at '{path}'. Skipping SkillSeeder.", skillsCsvPath);
    }

    // 3) Enemy baseline skills
    await EnemySkillSeeder.SeedDefaultsAsync(db /*, cts.Token*/);

    // 4) Enemies
    await EnemySeeder.SeedDefaultsAsync(db /*, cts.Token*/);

    // 5) Default Animas (Gray/Red/Green) with random skills/parts if available
    await AnimaSeeder.SeedDefaultsAsync(db /*, cts.Token*/);

    
    // Link table must exist; ensure you added DbSet<AnimaSkillEntity> + migration
    // If you had an old AnimaSeeder that created random animas, you can keep or remove it.
    // We'll deterministically add (or upsert) our 3 starters here:
    await AnimaStarterSeeder.SeedThreeStartersAsync(db);

    logger.LogInformation("✅ Seeding complete.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Seeding canceled.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"❌ Seeding failed: {ex.Message}");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
