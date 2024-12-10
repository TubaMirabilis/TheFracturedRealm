using Microsoft.EntityFrameworkCore;
using TheFracturedRealm.Application.Abstractions.Data.Models;

namespace TheFracturedRealm.Application.Abstractions.Data;

public interface IApplicationReadDbContext
{
    DbSet<CharacterReadModel> Characters { get; }
}
