namespace MindWeaver.Configuration;

/// <summary>
/// Identifies which relational database engine AppDbContext will target.
/// Add new values here as additional providers are introduced — the switch
/// in MauiProgram.cs is the only place that needs a matching case.
/// </summary>
public enum DatabaseProviderType
{
    /// <summary>SQLite — the default, zero-config, file-based engine.</summary>
    Sqlite,

    /// <summary>Microsoft SQL Server (or Azure SQL / LocalDB).</summary>
    SqlServer
}
