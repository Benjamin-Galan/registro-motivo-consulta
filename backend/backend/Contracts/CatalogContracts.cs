using System.ComponentModel.DataAnnotations;

namespace RegistroConsultas.Api.Contracts;

public sealed record SaveCatalogRequest(
    [Required, StringLength(20, MinimumLength = 1), RegularExpression("^[A-Za-z0-9_-]+$")]
    string Codigo,
    [Required, StringLength(120, MinimumLength = 1)]
    string Nombre);

public sealed record ChangeCatalogStatusRequest([Required] bool? Activo);

public sealed record CatalogResponse(
    long Id,
    string Codigo,
    string Nombre,
    bool Activo,
    DateTimeOffset CreadoEl,
    DateTimeOffset? ModificadoEl);
