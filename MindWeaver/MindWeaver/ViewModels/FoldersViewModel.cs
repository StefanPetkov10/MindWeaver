using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MindWeaver.Data;
using MindWeaver.Models;

namespace MindWeaver.ViewModels;

/// <summary>
/// Manages the sidebar folder list. Registered as a Singleton so the loaded
/// collection is shared across all Blazor components without redundant DB round-trips.
/// </summary>
public partial class FoldersViewModel : BaseViewModel
{
    private readonly AppDbContext _db;

    public FoldersViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Folders";
    }

    // ── Observable State ───────────────────────────────────────────────────────

    /// <summary>
    /// Flat, ordered list of all folders including their nested notes.
    /// Populated by <see cref="LoadFoldersCommand"/>.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Folder> _folders = new();

    /// <summary>
    /// Flat list of every note across all folders (plus unfiled notes).
    /// Required by <c>MudDropContainer</c> which needs a single source collection
    /// to render and match items against drop zones.
    /// </summary>
    public ObservableCollection<Note> AllNotes { get; } = new();

    // ── Commands ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads all folders from SQLite, eagerly including child Notes,
    /// ordered by <see cref="Folder.OrderIndex"/> for drag-and-drop consistency.
    /// Guarded by IsBusy to prevent concurrent executions.
    /// </summary>
    [RelayCommand]
    public async Task LoadFoldersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var loaded = await _db.Folders
                .Include(f => f.Notes)
                .OrderBy(f => f.OrderIndex)
                .AsNoTracking()
                .ToListAsync();

            // Unfiled notes — belong to no folder and must also appear in AllNotes
            // so the "Unfiled" drop zone can receive them.
            var unfiledNotes = await _db.Notes
                .Where(n => n.FolderId == null)
                .AsNoTracking()
                .ToListAsync();

            // ── Folders ────────────────────────────────────────────────────────
            // Replace in-place to preserve existing ObservableCollection bindings.
            Folders.Clear();
            foreach (var folder in loaded)
                Folders.Add(folder);

            // ── AllNotes (flat) ────────────────────────────────────────────────
            AllNotes.Clear();
            foreach (var folder in loaded)
                foreach (var note in folder.Notes)
                    AllNotes.Add(note);
            foreach (var note in unfiledNotes)
                AllNotes.Add(note);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Persists a new folder and refreshes the list.
    /// </summary>
    [RelayCommand]
    public async Task AddFolderAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            OrderIndex = Folders.Count   // append to end
        };

        _db.Folders.Add(folder);
        await _db.SaveChangesAsync();
        await LoadFoldersAsync();
    }

    /// <summary>
    /// Persists reordered <see cref="Folder.OrderIndex"/> values after a drag-and-drop
    /// operation. Call this with the updated, reordered list from the UI.
    /// </summary>
    [RelayCommand]
    public async Task SaveOrderAsync(IList<Folder> reordered)
    {
        for (int i = 0; i < reordered.Count; i++)
            reordered[i].OrderIndex = i;

        _db.Folders.UpdateRange(reordered);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Moves a note to a different folder (or to Unfiled when
    /// <paramref name="newFolderId"/> is <see langword="null"/>).
    ///
    /// Uses EF Core's <c>ExecuteUpdateAsync</c> (bulk-update API) so we never
    /// need to attach the detached entity that came from an <c>AsNoTracking</c>
    /// query — avoids the tracking-conflict exception that would occur inside
    /// a Singleton-lifetime DbContext.
    /// </summary>
    public async Task MoveNoteAsync(Note note, Guid? newFolderId)
    {
        // 1. Persist the change directly in the database.
        await _db.Notes
            .Where(n => n.Id == note.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.FolderId,  newFolderId)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

        // 2. Mirror the change on the in-memory object so the UI stays
        //    consistent without requiring a full reload from the DB.
        note.FolderId  = newFolderId;
        note.UpdatedAt = DateTime.UtcNow;
    }
}
