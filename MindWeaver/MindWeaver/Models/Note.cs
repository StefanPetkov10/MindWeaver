using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MindWeaver.Attributes;

namespace MindWeaver.Models;

public class Note
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    [Searchable("Note Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Searchable("Created On")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Nullable FK — a Note can be unorganized (not inside any Folder)
    public Guid? FolderId { get; set; }

    [ForeignKey(nameof(FolderId))]
    public Folder? Folder { get; set; }

    // One-to-many: a Note has many revision snapshots
    public ICollection<NoteRevision> Revisions { get; set; } = new List<NoteRevision>();
}
