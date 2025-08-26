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
    }
}