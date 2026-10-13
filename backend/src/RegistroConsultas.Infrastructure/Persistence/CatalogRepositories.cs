using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RegistroConsultas.Application.Abstractions.Catalogs;
using RegistroConsultas.Domain.Entities;
using RegistroConsultas.Domain.Exceptions;

namespace RegistroConsultas.Infrastructure.Persistence;

internal sealed class AreaRepository(RegistroDbContext dbContext) : IAreaRepository
{
    public async Task<IReadOnlyList<Area>> ListAsync(bool incluirAnulados, CancellationToken cancellationToken)
    {
        var query = dbContext.Areas.AsNoTracking();
        if (!incluirAnulados)
        {
            query = query.Where(area => !area.RegAnulado);
        }

        return await query.OrderBy(area => area.Nombre).ToArrayAsync(cancellationToken);
    }

    public Task<Area?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        dbContext.Areas.SingleOrDefaultAsync(area => area.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, long? excludingId, CancellationToken cancellationToken) =>
        dbContext.Areas.AnyAsync(area => area.CodigoArea == code && (!excludingId.HasValue || area.Id != excludingId), cancellationToken);

    public void Add(Area area) => dbContext.Areas.Add(area);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (CatalogRepositoryErrors.IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateCatalogCodeException("áreas", "el código indicado");
        }
    }
}

internal sealed class ZonaFilialRepository(RegistroDbContext dbContext) : IZonaFilialRepository
{
    public async Task<IReadOnlyList<ZonaFilial>> ListAsync(bool incluirAnulados, CancellationToken cancellationToken)
    {
        var query = dbContext.ZonasFiliales.AsNoTracking();
        if (!incluirAnulados)
        {
            query = query.Where(zona => !zona.RegAnulado);
        }

        return await query.OrderBy(zona => zona.Nombre).ToArrayAsync(cancellationToken);
    }

    public Task<ZonaFilial?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        dbContext.ZonasFiliales.SingleOrDefaultAsync(zona => zona.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, long? excludingId, CancellationToken cancellationToken) =>
        dbContext.ZonasFiliales.AnyAsync(zona => zona.CodigoZona == code && (!excludingId.HasValue || zona.Id != excludingId), cancellationToken);

    public void Add(ZonaFilial zona) => dbContext.ZonasFiliales.Add(zona);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (CatalogRepositoryErrors.IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateCatalogCodeException("zonas/filiales", "el código indicado");
        }
    }
}

internal static class CatalogRepositoryErrors
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var cause = exception.InnerException; cause is not null; cause = cause.InnerException)
        {
            if (cause is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }
}
