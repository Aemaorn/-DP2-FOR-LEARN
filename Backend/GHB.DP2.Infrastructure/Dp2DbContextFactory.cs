namespace GHB.DP2.Infrastructure;

using CommandLine;
using GHB.DP2.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class Dp2DbContextFactory : IDesignTimeDbContextFactory<Dp2DbContext>
{
    public Dp2DbContext CreateDbContext(string[] args)
    {
        var arguments = Parser.Default.ParseArguments<MigrationArguments>(args).Value;

        if (string.IsNullOrWhiteSpace(arguments.ConnectionString))
        {
            throw new ArgumentNullException(nameof(arguments.ConnectionString));
        }

        var optionsBuilder = new DbContextOptionsBuilder<Dp2DbContext>();
        optionsBuilder.UseNpgsql(arguments.ConnectionString);

        return new Dp2DbContext(optionsBuilder.Options, null!);
    }
}
