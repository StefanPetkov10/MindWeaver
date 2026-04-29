using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MindWeaver.Models;

public class NoteRevision
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public Guid NoteId { get; set; }

    [Required]
    public string ContentSnapshot { get; set; } = string.Empty;

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    // Navigation property back to the parent Note
    [ForeignKey(nameof(NoteId))]
    public Note Note { get; set; } = null!;
}
