using Microsoft.EntityFrameworkCore;

namespace MatchDay.Catalog.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Stadium> Stadiums => Set<Stadium>();

    public DbSet<Section> Sections => Set<Section>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<SectionPrice> SectionPrices => Set<SectionPrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stadium>(stadium =>
        {
            stadium.Property(s => s.Name).HasMaxLength(100);
            stadium.Property(s => s.City).HasMaxLength(60);
            stadium.HasMany(s => s.Sections).WithOne().HasForeignKey(s => s.StadiumId);
        });

        modelBuilder.Entity<Section>(section =>
        {
            section.Property(s => s.Name).HasMaxLength(60);
            section.ToTable(t => t.HasCheckConstraint("CK_Section_Layout", "\"Rows\" > 0 AND \"SeatsPerRow\" > 0"));
        });

        modelBuilder.Entity<Match>(match =>
        {
            match.Property(m => m.HomeTeam).HasMaxLength(60);
            match.Property(m => m.AwayTeam).HasMaxLength(60);
            match.Property(m => m.Competition).HasMaxLength(60);
            match.HasIndex(m => m.KickOff);
        });

        modelBuilder.Entity<SectionPrice>(price =>
        {
            price.HasKey(p => new { p.MatchId, p.SectionId });
            price.Property(p => p.Amount).HasPrecision(10, 2);
            price.HasOne(p => p.Match).WithMany(m => m.Prices).HasForeignKey(p => p.MatchId);
            price.HasOne(p => p.Section).WithMany().HasForeignKey(p => p.SectionId);
        });

        SeedData.Apply(modelBuilder);
    }
}
