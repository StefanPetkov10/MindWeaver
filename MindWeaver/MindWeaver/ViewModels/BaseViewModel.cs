using CommunityToolkit.Mvvm.ComponentModel;

namespace MindWeaver.ViewModels;

/// <summary>
/// Root base class for all ViewModels in MindWeaver.
/// Provides shared busy-state tracking via the CommunityToolkit.Mvvm source generator.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    /// <summary>
    /// True while an async operation is in progress.
    /// Exposes the generated <c>IsBusy</c> property for data binding and UI gating.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Convenience title that concrete ViewModels can override to label pages/dialogs.
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;
}
