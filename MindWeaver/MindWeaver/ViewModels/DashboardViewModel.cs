using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MindWeaver.Data;
using MindWeaver.Models;

namespace MindWeaver.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly AppDbContext _db;

    private static readonly (string Type, int Index)[] DefaultWidgets =
    [
        ("Welcome",      0),
        ("QuickActions", 1),
        ("Stats",        2),
    ];

    public DashboardViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Dashboard";
    }

    [ObservableProperty]
    private ObservableCollection<DashboardWidget> _widgets = new();

    [ObservableProperty]
    private int _noteCount;

    [ObservableProperty]
    private int _folderCount;

    public async Task LoadWidgetsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Изрично указваме да ползва EF Core, за да няма CS0411
            var count = await EntityFrameworkQueryableExtensions.CountAsync(_db.DashboardWidgets);

            if (count == 0)
                await SeedDefaultWidgetsAsync();

            var loaded = await _db.DashboardWidgets
                .Where(w => w.IsVisible)
                .OrderBy(w => w.PositionIndex)
                .AsNoTracking()
                .ToListAsync();

            Widgets.Clear();
            foreach (var widget in loaded)
                Widgets.Add(widget);

            NoteCount    = await EntityFrameworkQueryableExtensions.CountAsync(_db.Notes);
            FolderCount  = await EntityFrameworkQueryableExtensions.CountAsync(_db.Folders);

            // Seed a default folder so the Organizer and sidebar are never blank.
            await CreateDefaultFolderAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Creates a "General" inbox folder on first run if no folders exist yet.
    /// Idempotent — safe to call on every startup.
    /// </summary>
    public async Task CreateDefaultFolderAsync()
    {
        var anyFolders = await EntityFrameworkQueryableExtensions.AnyAsync(_db.Folders);
        if (anyFolders) return;

        _db.Folders.Add(new Folder
        {
            Id         = Guid.NewGuid(),
            Name       = "General",
            Icon       = "inbox",
            OrderIndex = 0,
        });

        await _db.SaveChangesAsync();
    }

    public async Task ReorderWidgetsAsync(DashboardWidget item, int newIndex)
    {
        Widgets.Remove(item);

        var clampedIndex = Math.Clamp(newIndex, 0, Widgets.Count);
        Widgets.Insert(clampedIndex, item);

        for (int i = 0; i < Widgets.Count; i++)
            Widgets[i].PositionIndex = i;

        foreach (var widget in Widgets)
        {
            var capturedIndex = widget.PositionIndex;
            var capturedId = widget.Id;

            await _db.DashboardWidgets
                .Where(w => w.Id == capturedId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(w => w.PositionIndex, capturedIndex));
        }
    }

    private async Task SeedDefaultWidgetsAsync()
    {
        var seeds = DefaultWidgets.Select(d => new DashboardWidget
        {
            Id = Guid.NewGuid(),
            WidgetType = d.Type,
            PositionIndex = d.Index,
            IsVisible = true,
        });

        _db.DashboardWidgets.AddRange(seeds);
        await _db.SaveChangesAsync();
    }
}