using Alibi.Domain;
using Microsoft.EntityFrameworkCore;

namespace Alibi.Infrastructure.Persistence;

public class AlibiDbContext(DbContextOptions<AlibiDbContext> options) : DbContext(options)
{
    public DbSet<MysteryCase> Cases => Set<MysteryCase>();
    public DbSet<Suspect> Suspects => Set<Suspect>();
    public DbSet<TimelineEvent> Timeline => Set<TimelineEvent>();
    public DbSet<AlibiClaim> Alibis => Set<AlibiClaim>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<Statement> Statements => Set<Statement>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<MysteryCase>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasMany(c => c.Suspects).WithOne().HasForeignKey(s => s.CaseId);
            e.HasMany(c => c.Evidence).WithOne().HasForeignKey(x => x.CaseId);
        });

        b.Entity<Suspect>(e =>
        {
            e.HasMany(s => s.Alibi).WithOne().HasForeignKey(a => a.SuspectId).OnDelete(DeleteBehavior.NoAction);
            e.HasMany(s => s.Statements).WithOne().HasForeignKey(s => s.SuspectId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<TimelineEvent>(e =>
        {
            e.HasOne<Suspect>().WithMany().HasForeignKey(t => t.SuspectId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(t => new { t.CaseId, t.SuspectId, t.Time }).IsUnique();
        });

        b.Entity<AlibiClaim>(e => e.HasIndex(a => new { a.CaseId, a.SuspectId, a.Time }).IsUnique());

        b.Entity<Evidence>(e => e.HasOne<Suspect>().WithMany().HasForeignKey(x => x.SuspectId).OnDelete(DeleteBehavior.NoAction));

        b.Entity<Statement>(e =>
        {
            // SQL Server 2025: coluna nativa VECTOR(256)
            e.Property(s => s.Embedding).HasColumnType($"vector({TextEmbedder.Dimensions})");
            // Índice vetorial aproximado (DiskANN) usado por VECTOR_SEARCH
#pragma warning disable EF9105 // índices vetoriais são experimentais no EF Core 11
            e.HasVectorIndex(s => s.Embedding, "IX_Statements_Embedding").HasMetric("cosine");
#pragma warning restore EF9105
        });
    }
}
