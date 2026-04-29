using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MindWeaver.Configuration;

namespace MindWeaver.Data;

// Only used by EF CLI tools (dotnet ef migrations add/update).
// Never instantiated at runtime — MAUI's FileSystem API is unavailable at design time.
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Pass the provider as a CLI arg: `dotnet ef migrations add ... -- sqlite|sqlserver`
    public AppDbContext CreateDbContext(string[] args)
    {
        var providerArg = args.FirstOrDefault() ?? "sqlite";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        if (providerArg.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\mssqllocaldb;Database=MindWeaverDb;Trusted_Connection=True;",
                o => o.MigrationsHistoryTable("__EFMigrationsHistory_SqlServer")
            );
        }
        else
        {
            // SQLite: use a simple local path — good enough for design time.
            optionsBuilder.UseSqlite(
                "Data Source=mindweaver_designtime.db",
                o => o.MigrationsHistoryTable("__EFMigrationsHistory_Sqlite")
            );
        }

        return new AppDbContext(optionsBuilder.Options);
    }
}
