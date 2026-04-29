using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MindWeaver.Models;

public class DashboardWidget
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// Discriminator string identifying the widget component to render.
    /// Examples: "RecentNotes", "Pomodoro", "QuickCapture", "Statistics".
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string WidgetType { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based display order used for drag-and-drop layout persistence.
    /// </summary>
    public int PositionIndex { get; set; } = 0;

    /// <summary>
    /// Controls whether the widget is rendered in the dashboard.
    /// </summary>
    public bool IsVisible { get; set; } = true;
}
