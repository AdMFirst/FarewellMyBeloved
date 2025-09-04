using Microsoft.EntityFrameworkCore;
using FarewellMyBeloved.Models;

namespace FarewellMyBeloved.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FarewellPerson> FarewellPeople { get; set; }
    public DbSet<FarewellMessage> FarewellMessages { get; set; }
    public DbSet<ContentReport> ContentReports { get; set; }
    public DbSet<ModeratorLog> ModeratorLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FarewellPerson entity
        modelBuilder.Entity<FarewellPerson>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(5000);
            
            entity.Property(e => e.PortraitUrl)
                .HasMaxLength(500);
            
            entity.Property(e => e.BackgroundUrl)
                .HasMaxLength(500);
            
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasAnnotation("EmailAddress", true);
            
            entity.Property(e => e.IsPublic)
                .IsRequired();
            
            // Create unique index on Slug
            entity.HasIndex(e => e.Slug)
                .IsUnique();
        });

        // Configure FarewellMessage entity
        modelBuilder.Entity<FarewellMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AuthorName)
                .HasMaxLength(100);
            
            entity.Property(e => e.AuthorEmail)
                .HasMaxLength(255)
                .HasAnnotation("EmailAddress", true);
            
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(2000);
            
            entity.Property(e => e.IsPublic)
                .IsRequired();

            // Configure relationship with FarewellPerson
            entity.HasOne(e => e.FarewellPerson)
                .WithMany(p => p.Messages)
                .HasForeignKey(e => e.FarewellPersonId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Create indexes for performance
            entity.HasIndex(e => e.FarewellPersonId);
            
            entity.HasIndex(e => e.CreatedAt)
                .IsDescending();
        });

        // Configure ContentReport entity
        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasAnnotation("EmailAddress", true);
            
            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Explanation)
                .HasMaxLength(2000);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.ResolvedAt);
            
            // Create indexes for performance
            entity.HasIndex(e => e.FarewellPersonId);
            entity.HasIndex(e => e.FarewellMessageId);
            entity.HasIndex(e => e.Reason);
            entity.HasIndex(e => e.CreatedAt)
                .IsDescending();
        });

        // Configure ModeratorLog entity
        modelBuilder.Entity<ModeratorLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ModeratorName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.TargetType)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Reason)
                .HasMaxLength(200);
            
            entity.Property(e => e.Details)
                .IsRequired()
                .HasMaxLength(2000);
            
            entity.Property(e => e.ContentReportId);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            // Configure relationship with ContentReport
            entity.HasOne(e => e.ContentReport)
                .WithMany(r => r.ModeratorLogs)
                .HasForeignKey(e => e.ContentReportId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Create indexes for performance
            entity.HasIndex(e => e.TargetType);
            entity.HasIndex(e => e.TargetId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.ModeratorName);
            entity.HasIndex(e => e.CreatedAt)
                .IsDescending();
        });
    }
}