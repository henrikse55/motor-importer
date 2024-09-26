using Importer.Entries;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Importer.FasterKv;

public sealed class MotorEntry
{
    public Guid Key { get; set; }
    public string ObjectName { get; set; }
    public Dictionary<string, string> Relations { get; set; } = default!;
    public Dictionary<string, string> RelatedArrays { get; set; } = default!;
    public Dictionary<string, string> Fields { get; set; } = default!;
}

public sealed class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions options) : base(options)
    {
        ChangeTracker.AutoDetectChangesEnabled = false;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<MotorEntry> MotorEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MotorEntry>(x =>
        {
            x.HasKey(e => e.Key);
            x.Property(e => e.Fields)
                .ToJson();
            x.Property(e => e.Relations)
                .ToJson();
            x.Property(e => e.RelatedArrays)
                .ToJson();
        });
        
        // modelBuilder.Entity<MotorEntry>(model =>
        // {
        //     model.HasKey(x => x.Key);
        //     model.OwnsOne(x => x.Content, owned =>
        //     {
        //         owned.ToJson();
        //         owned.Ignore(x => x.Key);
        //     });
        // });
    }
}