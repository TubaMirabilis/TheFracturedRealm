using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TheFracturedRealm.Application.Abstractions.Data;
using TheFracturedRealm.Application.Abstractions.Data.Models;
using TheFracturedRealm.Domain;

namespace TheFracturedRealm.Infrastructure;

public class ApplicationWriteDbContext : DbContext, IUnitOfWork
{
    public ApplicationWriteDbContext(DbContextOptions<ApplicationWriteDbContext> options) : base(options)
    {
    }
    public DbSet<Character> Characters { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationWriteDbContext).Assembly, WriteConfigurationsFilter);
    private static bool WriteConfigurationsFilter(Type type) => type.FullName?.Contains("Configurations.Write", StringComparison.OrdinalIgnoreCase) ?? false;
    public async Task<IDbTransaction> BeginTransactionAsync() => (await Database.BeginTransactionAsync()).GetDbTransaction();
}
