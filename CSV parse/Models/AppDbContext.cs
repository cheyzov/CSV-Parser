using Microsoft.EntityFrameworkCore;

namespace CSV_parse.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Value> Values { get; set; }

    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Value>(entity =>
        {
            entity.ToTable("Values");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Date)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.ExecutionTime)
                .IsRequired();

            entity.Property(e => e.IndicatorValue)
                .HasColumnName("Value")
                .IsRequired();

            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => new { e.FileName, e.Date });
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(e => e.FileName)
                .IsUnique();

            entity.Property(e => e.FirstOperationDate)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });
    }
}
