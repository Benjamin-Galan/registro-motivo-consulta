using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistroConsultas.Api.Contracts;
using RegistroConsultas.Application.Features.Catalogs;

namespace RegistroConsultas.Api.Controllers;

[ApiController]
[Route("api/v1/zonas-filiales")]
[Authorize]
public sealed class ZonasFilialesController(ICatalogManagementService catalogs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatalogResponse>>> List(
        [FromQuery] bool incluirAnulados = false,
        CancellationToken cancellationToken = default)
    {
        if (incluirAnulados && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        var items = await catalogs.ListZonasAsync(incluirAnulados, cancellationToken);
        return Ok(items.Select(Map).ToArray());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CatalogResponse>> GetById(
        long id,
        [FromQuery] bool incluirAnulado = false,
        CancellationToken cancellationToken = default)
    {
        if (incluirAnulado && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        return Ok(Map(await catalogs.GetZonaAsync(id, incluirAnulado, cancellationToken)));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CatalogResponse>> Create(
        SaveCatalogRequest request,
        CancellationToken cancellationToken)
    {
        var created = Map(await catalogs.CreateZonaAsync(request.Codigo, request.Nombre, cancellationToken));
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<CatalogResponse>> Update(
        long id,
        SaveCatalogRequest request,
        CancellationToken cancellationToken)
    {
        var updated = Map(await catalogs.UpdateZonaAsync(id, request.Codigo, request.Nombre, cancellationToken));
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await catalogs.SetZonaActiveAsync(id, activo: false, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:long}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> SetStatus(
        long id,
        ChangeCatalogStatusRequest request,
        CancellationToken cancellationToken)
    {
        await catalogs.SetZonaActiveAsync(id, request.Activo!.Value, cancellationToken);
        return NoContent();
    }

    private static CatalogResponse Map(CatalogItem item) =>
        new(item.Id, item.Codigo, item.Nombre, item.Activo, item.CreadoEl, item.ModificadoEl);
}
