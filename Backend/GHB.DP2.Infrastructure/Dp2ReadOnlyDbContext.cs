namespace GHB.DP2.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Read-only DbContext for parallel query operations.
/// Inherits all DbSet properties from Dp2DbContext but uses separate options for factory registration.
/// </summary>
public class Dp2ReadOnlyDbContext : Dp2DbContext
{
    public Dp2ReadOnlyDbContext()
    {
    }

    public Dp2ReadOnlyDbContext(
        DbContextOptions<Dp2ReadOnlyDbContext> options)
        : base(CreateBaseOptions(options), NullLogger<Dp2DbContext>.Instance)
    {
    }

    private static DbContextOptions<Dp2DbContext> CreateBaseOptions(DbContextOptions<Dp2ReadOnlyDbContext> options)
    {
        var builder = new DbContextOptionsBuilder<Dp2DbContext>();

        // Copy extensions from the derived options to the base options
        foreach (var extension in options.Extensions)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
        }

        return builder.Options;
    }
}