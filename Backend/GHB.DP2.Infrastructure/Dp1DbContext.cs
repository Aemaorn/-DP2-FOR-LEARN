namespace GHB.DP2.Infrastructure;

using CommandLine;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Infrastructure.EntityConfiguration.Plan;
using GHB.DP2.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class Dp1DbContext :
    DbContext,
    IDesignTimeDbContextFactory<Dp1DbContext>
{
    public Dp1DbContext()
    {
    }

    public Dp1DbContext(DbContextOptions<Dp1DbContext> options)
        : base(options)
    {
    }

    public DbSet<PlanViewGHBDP> PlanViewGhbdp { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PlanViewGhbdpConfiguration());
    }

    public Dp1DbContext CreateDbContext(string[] args)
    {
        var arguments = Parser.Default.ParseArguments<MigrationArguments>(args).Value;

        if (string.IsNullOrWhiteSpace(arguments.ConnectionString))
        {
            throw new ArgumentNullException(nameof(arguments.ConnectionString));
        }

        var optionsBuilder = new DbContextOptionsBuilder<Dp1DbContext>();
        optionsBuilder.UseNpgsql(arguments.ConnectionString)
                      .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return new Dp1DbContext(optionsBuilder.Options);
    }
}