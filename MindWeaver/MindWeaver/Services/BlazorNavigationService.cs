using Microsoft.AspNetCore.Components;
using MindWeaver.Contracts;

namespace MindWeaver.Services;

/// <summary>
/// <see cref="INavigationService"/> implementation for Blazor components
/// running inside the WebView. Wraps ASP.NET Core's <see cref="NavigationManager"/>.
///
/// Lifetime: <b>Scoped</b> — <see cref="NavigationManager"/> is itself Scoped
/// in the Blazor DI container, so this service must match that lifetime.
/// </summary>
public sealed class BlazorNavigationService : INavigationService
{
    private readonly NavigationManager _nav;

    public BlazorNavigationService(NavigationManager nav)
        => _nav = nav;

    /// <inheritdoc/>
    public Task NavigateToAsync(string route)
    {
        _nav.NavigateTo(route);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task GoBackAsync()
    {
        // Blazor WebView does not expose a native "go-back" stack via
        // NavigationManager, so we navigate to the root as a safe fallback.
        // Swap this with JS interop (history.back()) when you need true
        // browser-history behaviour.
        _nav.NavigateTo("/");
        return Task.CompletedTask;
    }
}
