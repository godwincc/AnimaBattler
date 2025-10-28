#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AnimaBattler.Data;

public sealed class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
{
    public GameDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("PETCCG_CONN")
                  ?? "Host=localhost;Port=5432;Database=petccg;Username=petccg;Password=petccg_pw;Pooling=true;Include Error Detail=true";
        var builder = new DbContextOptionsBuilder<GameDbContext>()
            .UseNpgsql(conn);
        return new GameDbContext(builder.Options);
    }
}
