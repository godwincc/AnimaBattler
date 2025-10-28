#nullable enable
using Microsoft.EntityFrameworkCore;
using AnimaBattler.Core.Anima;

namespace AnimaBattler.Data;

public sealed class GameDbContext : DbContext
{
    public DbSet<ArchetypeEntity> Archetypes => Set<ArchetypeEntity>();
    public DbSet<AnimaEntity> Animas => Set<AnimaEntity>();
    public DbSet<SkillEntity> Skills => Set<SkillEntity>();
  
    public DbSet<EnemyEntity> Enemies => Set<EnemyEntity>();
    public DbSet<EnemySkillEntity> EnemySkills => Set<EnemySkillEntity>();

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // --- Archetypes ---
        b.Entity<ArchetypeEntity>(e =>
        {
            e.ToTable("archetypes");
            e.HasKey(x => x.Id);

            e.Property(x => x.Color).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.BaseHp).IsRequired();
            e.Property(x => x.BaseSpeed).IsRequired();
            e.Property(x => x.DamageMult).HasPrecision(5, 2).IsRequired();
            e.Property(x => x.DefenseMult).HasPrecision(5, 2).IsRequired();
            e.Property(x => x.Description);
        });

        // --- Skills ---
        b.Entity<SkillEntity>(e =>
        {
            e.ToTable("skills");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.Target).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.Energy).HasDefaultValue(0).IsRequired();
            e.Property(x => x.BaseDamage).IsRequired();
            e.Property(x => x.EffectValue).HasPrecision(10, 2).IsRequired();
            e.Property(x => x.DurationTurns).IsRequired();
            e.Property(x => x.Description);

            // Required FK to archetypes
            e.Property(x => x.ArchetypeId).IsRequired();
            e.HasOne(x => x.Archetype)
             .WithMany() // (add a collection on ArchetypeEntity later if you want)
             .HasForeignKey(x => x.ArchetypeId)
             .OnDelete(DeleteBehavior.Cascade);

            // Uniqueness: each archetype cannot have duplicate skill codes
            e.HasIndex(x => new { x.ArchetypeId, x.Code }).IsUnique();

            // Helpful query index
            e.HasIndex(x => x.ArchetypeId);
        });

        b.Entity<EnemyEntity>(e =>
            {
                e.ToTable("enemies");
                e.HasKey(x => x.Id);

                e.Property(x => x.Code).HasMaxLength(64).IsRequired();
                e.HasIndex(x => x.Code).IsUnique();

                e.Property(x => x.Color)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                e.Property(x => x.Role)
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                e.Property(x => x.Level).IsRequired();
                e.Property(x => x.Hp).IsRequired();
                e.Property(x => x.DamageMultiplier).HasPrecision(5, 2).IsRequired();
                e.Property(x => x.Description);
            });

         b.Entity<EnemySkillEntity>(e =>
        {
            e.ToTable("enemy_skills");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(x => x.Target).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.BaseDamage).IsRequired();
            e.Property(x => x.EffectValue).HasPrecision(10, 2).IsRequired();
            e.Property(x => x.DurationTurns).IsRequired();
            e.Property(x => x.Description);
        });

         b.Entity<AnimaEntity>(e =>
        {
            e.ToTable("animas");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.Color)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            e.Property(x => x.Level).IsRequired();
            e.Property(x => x.Hp).IsRequired();
            e.Property(x => x.DamageMultiplier).HasPrecision(5, 2).IsRequired();

            e.Property(x => x.AssignedSkillCodes).HasMaxLength(512);
            e.Property(x => x.AssignedPartCodes).HasMaxLength(512);
            e.Property(x => x.Description);
        });
    }
}
