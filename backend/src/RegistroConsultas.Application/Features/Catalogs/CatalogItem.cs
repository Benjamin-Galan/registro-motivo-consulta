namespace RegistroConsultas.Application.Features.Catalogs;

public sealed record CatalogItem(
    long Id,
    string Codigo,
    string Nombre,
    bool Activo,
    DateTimeOffset CreadoEl,
    DateTimeOffset? ModificadoEl);
