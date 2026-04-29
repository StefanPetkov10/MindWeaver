using Microsoft.EntityFrameworkCore;
using MindWeaver.Models;

namespace MindWeaver.Data;

public class AppDbContext : DbContext
{

    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteRevision> NoteRevisions => Set<NoteRevision>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id).ValueGeneratedOnAdd();
            entity.Property(f => f.Name).IsRequired().HasMaxLength(128);
            entity.Property(f => f.Icon).HasMaxLength(64).HasDefaultValue("folder");

            // Folder → Notes (one-to-many)
            // Restrict: deleting a Folder that still contains Notes is blocked
            // at the DB level — callers must move/delete notes first.
            entity.HasMany(f => f.Notes)
                  .WithOne(n => n.Folder)
                  .HasForeignKey(n => n.FolderId)
                  .IsRequired(false)          // nullable FK — notes can be "unfiled"
                  .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).ValueGeneratedOnAdd();
            entity.Property(n => n.Title).IsRequired().HasMaxLength(256);
            entity.Property(n => n.Content).IsRequired();

            // Note → NoteRevisions (one-to-many)
            // Cascade: removing a Note automatically purges all its revisions.
            entity.HasMany(n => n.Revisions)
                  .WithOne(r => r.Note)
                  .HasForeignKey(r => r.NoteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<NoteRevision>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();
            entity.Property(r => r.ContentSnapshot).IsRequired();
        });


        modelBuilder.Entity<DashboardWidget>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Id).ValueGeneratedOnAdd();
            entity.Property(w => w.WidgetType).IsRequired().HasMaxLength(64);
            entity.Property(w => w.IsVisible).HasDefaultValue(true);
        });
    }
}
