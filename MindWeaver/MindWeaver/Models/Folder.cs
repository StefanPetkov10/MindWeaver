using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MindWeaver.Attributes;

namespace MindWeaver.Models;

public class Folder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(128)]
    [Searchable("Folder Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based position index used for drag-and-drop ordering in the sidebar.
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// Material Icon name (e.g. "folder", "star", "work") rendered by MudBlazor.
    /// </summary>
    [MaxLength(64)]
    public string Icon { get; set; } = "folder";

    // One-to-many: a Folder contains many Notes
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
