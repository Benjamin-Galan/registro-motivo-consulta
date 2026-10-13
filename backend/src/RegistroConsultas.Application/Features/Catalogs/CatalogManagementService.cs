using RegistroConsultas.Application.Abstractions.Catalogs;
using RegistroConsultas.Domain.Entities;
using RegistroConsultas.Domain.Exceptions;

namespace RegistroConsultas.Application.Features.Catalogs;

public sealed class CatalogManagementService(
    IAreaRepository areaRepository,
    IZonaFilialRepository zonaRepository,
    TimeProvider timeProvider) : ICatalogManagementService
{
    public async Task<IReadOnlyList<CatalogItem>> ListAreasAsync(bool incluirAnulados, CancellationToken cancellationToken) =>
        (await areaRepository.ListAsync(incluirAnulados, cancellationToken)).Select(Map).ToArray();

    public async Task<CatalogItem> GetAreaAsync(long id, bool incluirAnulado, CancellationToken cancellationToken) =>
        Map(await GetAreaOrThrowAsync(id, incluirAnulado, cancellationToken));

    public async Task<CatalogItem> CreateAreaAsync(string codigo, string nombre, CancellationToken cancellationToken)
    {
        var area = new Area(codigo, nombre, timeProvider.GetUtcNow());
        await EnsureAreaCodeAvailableAsync(area.CodigoArea, null, cancellationToken);
        areaRepository.Add(area);
        await areaRepository.SaveChangesAsync(cancellationToken);
        return Map(area);
    }

    public async Task<CatalogItem> UpdateAreaAsync(long id, string codigo, string nombre, CancellationToken cancellationToken)
    {
        var area = await GetAreaOrThrowAsync(id, true, cancellationToken);
        var normalizedCode = new Area(codigo, nombre, timeProvider.GetUtcNow()).CodigoArea;
        await EnsureAreaCodeAvailableAsync(normalizedCode, id, cancellationToken);
        area.CambiarDatos(codigo, nombre, timeProvider.GetUtcNow());
        await areaRepository.SaveChangesAsync(cancellationToken);
        return Map(area);
    }

    public async Task SetAreaActiveAsync(long id, bool activo, CancellationToken cancellationToken)
    {
        var area = await GetAreaOrThrowAsync(id, true, cancellationToken);
        area.CambiarActivo(activo, timeProvider.GetUtcNow());
        await areaRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogItem>> ListZonasAsync(bool incluirAnulados, CancellationToken cancellationToken) =>
        (await zonaRepository.ListAsync(incluirAnulados, cancellationToken)).Select(Map).ToArray();

    public async Task<CatalogItem> GetZonaAsync(long id, bool incluirAnulado, CancellationToken cancellationToken) =>
        Map(await GetZonaOrThrowAsync(id, incluirAnulado, cancellationToken));

    public async Task<CatalogItem> CreateZonaAsync(string codigo, string nombre, CancellationToken cancellationToken)
    {
        var zona = new ZonaFilial(codigo, nombre, timeProvider.GetUtcNow());
        await EnsureZonaCodeAvailableAsync(zona.CodigoZona, null, cancellationToken);
        zonaRepository.Add(zona);
        await zonaRepository.SaveChangesAsync(cancellationToken);
        return Map(zona);
    }

    public async Task<CatalogItem> UpdateZonaAsync(long id, string codigo, string nombre, CancellationToken cancellationToken)
    {
        var zona = await GetZonaOrThrowAsync(id, true, cancellationToken);
        var normalizedCode = new ZonaFilial(codigo, nombre, timeProvider.GetUtcNow()).CodigoZona;
        await EnsureZonaCodeAvailableAsync(normalizedCode, id, cancellationToken);
        zona.CambiarDatos(codigo, nombre, timeProvider.GetUtcNow());
        await zonaRepository.SaveChangesAsync(cancellationToken);
        return Map(zona);
    }

    public async Task SetZonaActiveAsync(long id, bool activo, CancellationToken cancellationToken)
    {
        var zona = await GetZonaOrThrowAsync(id, true, cancellationToken);
        zona.CambiarActivo(activo, timeProvider.GetUtcNow());
        await zonaRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Area> GetAreaOrThrowAsync(long id, bool includeArchived, CancellationToken cancellationToken)
    {
        var area = await areaRepository.GetByIdAsync(id, cancellationToken);
        if (area is null || (!includeArchived && area.RegAnulado))
        {
            throw new CatalogItemNotFoundException("el área", id);
        }

        return area;
    }

    private async Task<ZonaFilial> GetZonaOrThrowAsync(long id, bool includeArchived, CancellationToken cancellationToken)
    {
        var zona = await zonaRepository.GetByIdAsync(id, cancellationToken);
        if (zona is null || (!includeArchived && zona.RegAnulado))
        {
            throw new CatalogItemNotFoundException("la zona/filial", id);
        }

        return zona;
    }

    private async Task EnsureAreaCodeAvailableAsync(string code, long? excludingId, CancellationToken cancellationToken)
    {
        if (await areaRepository.CodeExistsAsync(code, excludingId, cancellationToken))
        {
            throw new DuplicateCatalogCodeException("áreas", code);
        }
    }

    private async Task EnsureZonaCodeAvailableAsync(string code, long? excludingId, CancellationToken cancellationToken)
    {
        if (await zonaRepository.CodeExistsAsync(code, excludingId, cancellationToken))
        {
            throw new DuplicateCatalogCodeException("zonas/filiales", code);
        }
    }

    private static CatalogItem Map(Area area) =>
        new(area.Id, area.CodigoArea, area.Nombre, !area.RegAnulado, area.CreadoEl, area.ModificadoEl);

    private static CatalogItem Map(ZonaFilial zona) =>
        new(zona.Id, zona.CodigoZona, zona.Nombre, !zona.RegAnulado, zona.CreadoEl, zona.ModificadoEl);
}
