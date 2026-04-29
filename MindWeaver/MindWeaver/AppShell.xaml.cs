using MindWeaver.Views;

namespace MindWeaver;

/// <summary>
/// Application Shell — the MAUI navigation root that replaces the bare
/// <see cref="MainPage"/> window root used before native routing was needed.
///
/// Responsibilities:
///   1. Host the existing Blazor WebView (<see cref="MainPage"/>) as the
///      Shell's default content — no visible UI change for the end-user.
///   2. Register all native XAML page routes so that
///      <c>Shell.Current.GoToAsync(nameof(NoteNativePage))</c> works without
///      throwing a <see cref="ArgumentException"/> about unknown routes.
///
/// Route registration must happen BEFORE any navigation call, so the
/// constructor is the correct place (runs once at app startup, before any
/// page is shown).
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // ── Native XAML page routes ───────────────────────────────────────────
        // Each route name matches nameof(PageClass) so callers can navigate
        // with NavigateToAsync(nameof(NoteNativePage)) — a refactor-safe
        // string that the compiler will catch if the class is renamed.
        Routing.RegisterRoute(nameof(NoteNativePage), typeof(NoteNativePage));
    }
}
