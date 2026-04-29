namespace MindWeaver.Contracts;

/// <summary>
/// Abstracts navigation so ViewModels remain completely decoupled from
/// both Blazor's NavigationManager and MAUI's Shell routing engine.
///
/// Two concrete implementations exist:
///   • <see cref="MindWeaver.Services.BlazorNavigationService"/> — used by
///     Blazor components inside the WebView.
///   • <see cref="MindWeaver.Services.MauiNavigationService"/> — used by
///     native XAML pages hosted by MAUI Shell.
///
/// The active implementation is resolved at runtime via the DI container,
/// meaning the same ViewModel works unchanged in both UI environments.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the given <paramref name="route"/>.
    /// </summary>
    /// <param name="route">
    /// A Blazor relative path (e.g. <c>"/notes/edit"</c>) or a MAUI Shell
    /// route (e.g. <c>"//NoteNativePage"</c>).
    /// </param>
    Task NavigateToAsync(string route);

    /// <summary>
    /// Navigates back one step in the navigation stack.
    /// </summary>
    Task GoBackAsync();
}
