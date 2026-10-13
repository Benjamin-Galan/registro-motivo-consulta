using RegistroConsultas.Domain.Entities;

namespace RegistroConsultas.Application.Abstractions.Catalogs;

public interface IZonaFilialRepository
{
    Task<IReadOnlyList<ZonaFilial>> ListAsync(bool incluirAnulados, CancellationToken cancellationToken);
    Task<ZonaFilial?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, long? excludingId, CancellationToken cancellationToken);
    void Add(ZonaFilial zona);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
