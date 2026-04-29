namespace MindWeaver.Configuration;

/// <summary>
/// Carries all database-related startup settings.
///
/// Registered as a <b>Singleton</b> in the DI container so that
/// <see cref="Data.AppDbContext"/> and any diagnostic tooling can resolve
/// the active provider without re-reading configuration on every request.
///
/// In production you would populate this from appsettings.json,
/// environment variables, or a platform-specific secure store.
/// For now the values are hardcoded in <c>MauiProgram.cs</c> so the
/// architecture is demonstrable without external config files.
/// </summary>
public sealed class DatabaseConfig
{
    /// <summary>
    /// The database engine that will be activated at runtime.
    /// Switch this value to <see cref="DatabaseProviderType.SqlServer"/>
    /// to route all EF Core operations to SQL Server instead of SQLite.
    /// </summary>
    public DatabaseProviderType Provider { get; init; } = DatabaseProviderType.Sqlite;

    /// <summary>
    /// Connection string used when <see cref="Provider"/> is
    /// <see cref="DatabaseProviderType.Sqlite"/>.
    ///
    /// At runtime this is replaced with the full sandboxed path via
    /// <c>FileSystem.AppDataDirectory</c> in <c>MauiProgram.cs</c>;
    /// the value here is the fallback / design-time default.
    /// </summary>
    public string SqliteConnectionString { get; init; } = "Data Source=app.db";

    /// <summary>
    /// Connection string used when <see cref="Provider"/> is
    /// <see cref="DatabaseProviderType.SqlServer"/>.
    ///
    /// LocalDB is the zero-install SQL Server instance available on every
    /// Windows developer machine via the Visual Studio workload.
    /// Swap this for a full SQL Server or Azure SQL connection string
    /// in staging / production environments.
    /// </summary>
    public string SqlServerConnectionString { get; init; } =
        @"Server=(localdb)\mssqllocaldb;Database=MindWeaverDb;Trusted_Connection=True;";
}
