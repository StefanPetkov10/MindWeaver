using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MindWeaver.Contracts;
using MindWeaver.Data;
using MindWeaver.Models;

namespace MindWeaver.ViewModels;

/// <summary>
/// Drives the note editor surface. Registered as Transient so each
/// editor page receives an isolated, fresh instance with no stale state.
/// </summary>
public partial class NoteViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly INavigationService _navigation;

    /// <summary>
    /// Accepts both the EF Core context for persistence and an
    /// <see cref="INavigationService"/> abstraction so that navigation
    /// calls work identically whether the ViewModel is bound to a Blazor
    /// component (resolved with <see cref="Services.BlazorNavigationService"/>)
    /// or a native MAUI XAML page (resolved with
    /// <see cref="Services.MauiNavigationService"/>).
    /// </summary>
    public NoteViewModel(AppDbContext db, INavigationService navigation)
    {
        _db         = db;
        _navigation = navigation;
        Title       = "New Note";
    }

    // ── Observable State ───────────────────────────────────────────────────────

    /// <summary>
    /// Null when creating a new note; set to the DB-assigned Guid after first save.
    /// </summary>
    [ObservableProperty]
    private Guid? _currentNoteId;

    [ObservableProperty]
    private string _noteTitle = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    // ── Undo / Redo ────────────────────────────────────────────────────────────

    // Stacks hold Content snapshots for in-memory undo/redo.
    // The undo stack is capped at 50 entries — any older entries are silently
    // discarded, which is fine for a text editor demo. For production you could
    // trim the bottom of the stack via a Queue+Stack hybrid or LinkedList.
    private const int UndoStackLimit = 50;
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();

    // Guards against recursive pushes when Undo/Redo programmatically sets Content.
    private bool _isUndoingOrRedoing;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// CommunityToolkit partial method hook — fires BEFORE the backing field
    /// is overwritten, so <c>Content</c> still holds the value being replaced.
    /// This is the correct place to snapshot for undo.
    /// </summary>
    partial void OnContentChanging(string? value)
    {
        // Skip when Undo/Redo themselves are driving the change, or when
        // the value hasn't actually changed (avoids spurious empty entries).
        if (_isUndoingOrRedoing || value == Content)
            return;

        // Enforce stack size cap — discard the oldest entry when full.
        if (_undoStack.Count >= UndoStackLimit)
        {
            var temp = _undoStack.ToArray();   // newest-first
            _undoStack.Clear();
            foreach (var entry in temp.Take(UndoStackLimit - 1).Reverse())
                _undoStack.Push(entry);
        }

        _undoStack.Push(Content);
        _redoStack.Clear();

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// The folder this note belongs to. Null = unfiled / Inbox.
    /// </summary>
    [ObservableProperty]
    private Guid? _selectedFolderId;

    /// <summary>
    /// Human-readable feedback message set after a save attempt.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // ── Navigation Commands ────────────────────────────────────────────────────

    /// <summary>
    /// Navigates back to the previous screen. Works from both Blazor pages
    /// and native XAML pages because it delegates to <see cref="INavigationService"/>.
    /// </summary>
    [RelayCommand]
    public Task GoBackAsync() => _navigation.GoBackAsync();

    // ── Commands ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Pops the most recent Content snapshot off the undo stack, pushes the
    /// current Content onto the redo stack, then restores the snapshot.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (!CanUndo) return;

        _isUndoingOrRedoing = true;
        try
        {
            _redoStack.Push(Content);
            Content = _undoStack.Pop();
        }
        finally
        {
            _isUndoingOrRedoing = false;
        }

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Pops the most recently undone snapshot off the redo stack, pushes the
    /// current Content onto the undo stack, then restores the snapshot.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (!CanRedo) return;

        _isUndoingOrRedoing = true;
        try
        {
            _undoStack.Push(Content);
            Content = _redoStack.Pop();
        }
        finally
        {
            _isUndoingOrRedoing = false;
        }

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Loads an existing note by id into the editor surface.
    /// </summary>
    [RelayCommand]
    public async Task LoadNoteAsync(Guid noteId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            var note = await _db.Notes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == noteId);

            if (note is null) return;

            CurrentNoteId    = note.Id;
            NoteTitle        = note.Title;
            Content          = note.Content;
            SelectedFolderId = note.FolderId;
            Title            = note.Title;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Persists the current editor state.
    ///
    /// Strategy:
    /// - <c>CurrentNoteId == null</c> → INSERT a new <see cref="Note"/>, capture its id.
    /// - <c>CurrentNoteId != null</c>  → UPDATE the tracked entity's fields.
    /// - In both branches: INSERT a <see cref="NoteRevision"/> snapshot so full history
    ///   is preserved regardless of path taken.
    /// - Single <c>SaveChangesAsync</c> call writes both rows in one transaction.
    /// </summary>
    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(NoteTitle))
        {
            StatusMessage = "Title cannot be empty.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = string.Empty;

            Note note;

            if (CurrentNoteId is null)
            {
                // ── INSERT path ────────────────────────────────────────────────
                note = new Note
                {
                    Id              = Guid.NewGuid(),
                    Title           = NoteTitle.Trim(),
                    Content         = Content,
                    FolderId        = SelectedFolderId,
                    CreatedAt       = DateTime.UtcNow,
                    UpdatedAt       = DateTime.UtcNow
                };

                _db.Notes.Add(note);
                CurrentNoteId = note.Id;
            }
            else
            {
                // ── UPDATE path ────────────────────────────────────────────────
                note = await _db.Notes.FindAsync(CurrentNoteId.Value)
                    ?? throw new InvalidOperationException(
                           $"Note '{CurrentNoteId}' was not found in the database.");

                note.Title     = NoteTitle.Trim();
                note.Content   = Content;
                note.FolderId  = SelectedFolderId;
                note.UpdatedAt = DateTime.UtcNow;
            }

            // ── Revision snapshot (always) ─────────────────────────────────────
            var revision = new NoteRevision
            {
                Id              = Guid.NewGuid(),
                NoteId          = note.Id,
                ContentSnapshot = Content,
                SavedAt         = DateTime.UtcNow
            };

            _db.NoteRevisions.Add(revision);

            // Single transaction — Note upsert + Revision insert committed together.
            await _db.SaveChangesAsync();

            Title         = note.Title;
            StatusMessage = $"Saved at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            throw;   // let the UI layer log/display if needed
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads all revision history for the current note, ordered newest first.
    /// </summary>
    [RelayCommand]
    public async Task<IReadOnlyList<NoteRevision>> LoadRevisionsAsync()
    {
        if (CurrentNoteId is null)
            return Array.Empty<NoteRevision>();

        return await _db.NoteRevisions
            .Where(r => r.NoteId == CurrentNoteId.Value)
            .OrderByDescending(r => r.SavedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Resets the ViewModel to a blank new-note state.
    /// </summary>
    public void Reset()
    {
        // Clear undo/redo history — stale snapshots from a previous note
        // must not survive into the blank new-note state.
        _isUndoingOrRedoing = true;
        try
        {
            _undoStack.Clear();
            _redoStack.Clear();
            CurrentNoteId    = null;
            NoteTitle        = string.Empty;
            Content          = string.Empty;
            SelectedFolderId = null;
            StatusMessage    = string.Empty;
            Title            = "New Note";
        }
        finally
        {
            _isUndoingOrRedoing = false;
        }

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }
}
