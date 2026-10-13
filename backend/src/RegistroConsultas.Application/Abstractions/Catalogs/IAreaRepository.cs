using RegistroConsultas.Domain.Entities;

namespace RegistroConsultas.Application.Abstractions.Catalogs;

public interface IAreaRepository
{
    Task<IReadOnlyList<Area>> ListAsync(bool incluirAnulados, CancellationToken cancellationToken);
    Task<Area?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, long? excludingId, CancellationToken cancellationToken);
    void Add(Area area);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
