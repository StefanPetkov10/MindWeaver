using MindWeaver.Contracts;

namespace MindWeaver.Services;

/// <summary>
/// <see cref="INavigationService"/> implementation for native MAUI XAML pages
/// routed through <see cref="Shell"/>. Wraps <c>Shell.Current.GoToAsync</c>.
///
/// Lifetime: <b>Singleton</b> — Shell is a singleton, and this service holds
/// no mutable state, so a single instance is safe for the app's lifetime.
/// </summary>
public sealed class MauiNavigationService : INavigationService
{
    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="route"/> must be a registered Shell route, e.g.
    /// <c>"//NoteNativePage"</c> for an absolute route or
    /// <c>"NoteNativePage"</c> for a relative push.
    /// </remarks>
    public Task NavigateToAsync(string route)
        => Shell.Current.GoToAsync(route);

    /// <inheritdoc/>
    public Task GoBackAsync()
        => Shell.Current.GoToAsync("..");
}
