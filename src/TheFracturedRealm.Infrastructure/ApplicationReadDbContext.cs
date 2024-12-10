using Microsoft.EntityFrameworkCore;
using TheFracturedRealm.Application.Abstractions.Data;
using TheFracturedRealm.Application.Abstractions.Data.Models;

namespace TheFracturedRealm.Infrastructure;

public class ApplicationReadDbContext : DbContext, IApplicationReadDbContext
{
    public ApplicationReadDbContext(DbContextOptions<ApplicationReadDbContext> options) : base(options)
    {
    }
    public DbSet<CharacterReadModel> Characters { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationReadDbContext).Assembly, ReadConfigurationsFilter);
    private static bool ReadConfigurationsFilter(Type type) => type.FullName?.Contains("Configurations.Read", StringComparison.OrdinalIgnoreCase) ?? false;
}
