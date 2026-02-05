/***
 * 
 * 
 * Licensed under GPL 3.0 License
 * 
 */

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace tyss.Services;


public class Folder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public string? Data { get; set; } = null;
    public int? ParentId { get; set; }
          
    public List<Folder> Children { get; set; } = new();

    public bool ShouldSerializeChildren()
    {
        return Children != null && Children.Count > 0;
    }

    [JsonIgnore]
    public Folder? Parent { get; set; }
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Folder> Folders => Set<Folder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {        
        modelBuilder.Entity<Folder>()
            .HasOne(f => f.Parent)
            .WithMany(f => f.Children)
            .HasForeignKey(f => f.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}