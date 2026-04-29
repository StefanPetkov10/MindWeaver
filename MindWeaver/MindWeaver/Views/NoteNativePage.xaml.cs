using MindWeaver.ViewModels;

namespace MindWeaver.Views;

/// <summary>
/// Code-behind for the native MAUI XAML "Focus Mode / Quick Capture" page.
///
/// Architecture note — why this is almost empty:
///   All state and logic live in <see cref="NoteViewModel"/>. The code-behind
///   has exactly one responsibility: receive the ViewModel from the DI container
///   and assign it as <see cref="BindableObject.BindingContext"/>.
///
///   This is the canonical MVVM pattern for MAUI:
///     • ViewModel ← injected by the DI container (no `new` keyword here).
///     • View       ← binds to ViewModel properties declaratively in XAML.
///     • No business logic, no navigation logic, no data access in this file.
/// </summary>
public partial class NoteNativePage : ContentPage
{
    private readonly NoteViewModel _viewModel;

    /// <summary>
    /// Constructs the page with a <see cref="NoteViewModel"/> provided by
    /// the MAUI dependency injection container (registered as Transient in
    /// <c>MauiProgram.cs</c>).
    ///
    /// Each navigation to this page creates a fresh ViewModel instance,
    /// guaranteeing no stale state from a previous edit session.
    /// </summary>
    /// <param name="viewModel">
    /// The note editor ViewModel. Injected — never instantiated manually.
    /// </param>
    public NoteNativePage(NoteViewModel viewModel)
    {
        InitializeComponent();

        _viewModel      = viewModel;
        BindingContext  = _viewModel;
    }

    // ── Optional lifecycle hooks ───────────────────────────────────────────────

    /// <summary>
    /// Focuses the Title Entry when the page first appears, so the user can
    /// start typing immediately without an extra tap.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Give the layout a single frame to settle before requesting focus,
        // which prevents the keyboard from flashing on some Android devices.
        Dispatcher.Dispatch(() => TitleEntry.Focus());
    }

    /// <summary>
    /// Clears soft-keyboard focus when leaving the page to avoid ghost-focus
    /// issues on return navigation on some platforms.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TitleEntry.Unfocus();
        ContentEditor.Unfocus();
    }
}
