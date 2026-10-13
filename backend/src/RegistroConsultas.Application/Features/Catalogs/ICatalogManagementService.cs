namespace RegistroConsultas.Application.Features.Catalogs;

public interface ICatalogManagementService
{
    Task<IReadOnlyList<CatalogItem>> ListAreasAsync(bool incluirAnulados, CancellationToken cancellationToken);
    Task<CatalogItem> GetAreaAsync(long id, bool incluirAnulado, CancellationToken cancellationToken);
    Task<CatalogItem> CreateAreaAsync(string codigo, string nombre, CancellationToken cancellationToken);
    Task<CatalogItem> UpdateAreaAsync(long id, string codigo, string nombre, CancellationToken cancellationToken);
    Task SetAreaActiveAsync(long id, bool activo, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItem>> ListZonasAsync(bool incluirAnulados, CancellationToken cancellationToken);
    Task<CatalogItem> GetZonaAsync(long id, bool incluirAnulado, CancellationToken cancellationToken);
    Task<CatalogItem> CreateZonaAsync(string codigo, string nombre, CancellationToken cancellationToken);
    Task<CatalogItem> UpdateZonaAsync(long id, string codigo, string nombre, CancellationToken cancellationToken);
    Task SetZonaActiveAsync(long id, bool activo, CancellationToken cancellationToken);
}
