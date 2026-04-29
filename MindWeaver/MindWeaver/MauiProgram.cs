using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindWeaver.Configuration;
using MindWeaver.Contracts;
using MindWeaver.Data;
using MindWeaver.Services;
using MindWeaver.ViewModels;
using MindWeaver.Views;
using MudBlazor.Services;

namespace MindWeaver
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();

            // ── Database Configuration ─────────────────────────────────────────────────
            // Singleton: configuration is read once at startup and never mutated.
            // Swap Provider to DatabaseProviderType.SqlServer (and supply a real
            // connection string) to route the entire app to SQL Server without
            // changing a single line of ViewModel or Model code.
            builder.Services.AddSingleton(new DatabaseConfig
            {
                Provider                 = DatabaseProviderType.Sqlite,
                SqliteConnectionString   = "Data Source=app.db",   // overridden below
                SqlServerConnectionString = @"Server=(localdb)\mssqllocaldb;Database=MindWeaverDb;Trusted_Connection=True;"
            });

            // ── EF Core DbContext (provider-switched) ────────────────────────────
            // The factory overload receives IServiceProvider so we can resolve
            // DatabaseConfig and branch on Provider — AppDbContext itself never
            // knows which engine is active (true DB-agnostic architecture).
            builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var config = serviceProvider.GetRequiredService<DatabaseConfig>();

                switch (config.Provider)
                {
                    case DatabaseProviderType.SqlServer:
                        // Requires: Microsoft.EntityFrameworkCore.SqlServer NuGet package
                        // Migrations folder: Migrations/SqlServer
                        options.UseSqlServer(
                            config.SqlServerConnectionString,
                            sqlOpts => sqlOpts.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                                              .MigrationsHistoryTable("__EFMigrationsHistory_SqlServer")
                        );
                        break;

                    case DatabaseProviderType.Sqlite:
                    default:
                        // Resolve the per-platform sandboxed data directory at runtime
                        // so the DB file lands in the correct location on every platform.
                        var dbPath = Path.Combine(
                            FileSystem.AppDataDirectory, "mindweaver.db");

                        // Migrations folder: Migrations/Sqlite
                        options.UseSqlite(
                            $"Data Source={dbPath}",
                            sqliteOpts => sqliteOpts.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                                                    .MigrationsHistoryTable("__EFMigrationsHistory_Sqlite")
                        );
                        break;
                }
            });

            // ── Shell & Root Pages ─────────────────────────────────────────────
            // Singleton: the window root is created once per app lifetime.
            // AppShell receives MainPage through DI so both are fully resolved.
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<AppShell>();

            // ── ViewModels ────────────────────────────────────────────────────
            // Singleton: folder list is global sidebar state, loaded once and
            //            shared across the entire app lifetime.
            builder.Services.AddSingleton<FoldersViewModel>();

            // Transient: each note editor gets a clean, isolated state machine.
            //            No risk of stale CurrentNoteId leaking between pages.
            builder.Services.AddTransient<NoteViewModel>();

            // Transient: dashboard always reloads widget order fresh on navigation.
            //            Must not be Singleton — holds a transient AppDbContext.
            builder.Services.AddTransient<DashboardViewModel>();

            // ── Plugin System ─────────────────────────────────────────────────
            // Singleton: plugin DLLs are discovered once per app session.
            // The Plugins folder lives at FileSystem.AppDataDirectory/Plugins.
            builder.Services.AddSingleton<PluginLoaderService>();

            // ── Navigation Services ───────────────────────────────────────────
            //
            // INavigationService has two concrete implementations:
            //
            //   • BlazorNavigationService  — wraps NavigationManager (Scoped).
            //     Injected into Blazor components via the WebView's own Scoped
            //     DI scope; NavigationManager is itself Scoped, so this must be
            //     Scoped too.
            //
            //   • MauiNavigationService    — wraps Shell.Current (Singleton).
            //     Used by native XAML pages resolved from the root DI container.
            //
            // Strategy: register MauiNavigationService as the default singleton
            // binding for INavigationService so that NoteViewModel (Transient)
            // resolves correctly from the MAUI root container when instantiated
            // for NoteNativePage.
            //
            // For Blazor pages, BlazorNavigationService is registered as Scoped
            // so that Blazor's Scoped container resolves it instead, overriding
            // the root registration within the WebView DI scope.
            builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
            builder.Services.AddScoped<BlazorNavigationService>();

            // ── Native XAML Views ─────────────────────────────────────────────
            // Transient: Shell creates a fresh page (and therefore a fresh
            // NoteViewModel) every time the route is navigated to, matching
            // the ViewModel's own Transient lifetime.
            builder.Services.AddTransient<NoteNativePage>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure the SQLite database and tables are created on startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            return app;
        }
    }
}
