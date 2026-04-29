namespace MindWeaver.Attributes;

/// <summary>
/// Marks a model property as a searchable field. The reflection engine in
/// <see cref="MindWeaver.Components.Shared.DynamicFilterForm{TItem}"/> will
/// discover every property decorated with this attribute and automatically
/// generate the matching input control in the UI.
///
/// Supported property types and their generated controls:
///   • string   → MudTextField (Contains search)
///   • DateTime → MudDatePicker (exact calendar-day match)
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SearchableAttribute : Attribute
{
    /// <summary>
    /// The human-readable label shown above the generated form control.
    /// </summary>
    public string Label { get; }

    /// <param name="label">
    /// Display label for the auto-generated input (e.g. "Note Title", "Created On").
    /// </param>
    public SearchableAttribute(string label)
    {
        Label = label;
    }
}
