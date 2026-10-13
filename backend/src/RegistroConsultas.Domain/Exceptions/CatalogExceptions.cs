namespace RegistroConsultas.Domain.Exceptions;

public sealed class CatalogItemNotFoundException(string catalog, long id)
    : Exception($"No se encontró {catalog} con identificador {id}.")
{
    public string Catalog { get; } = catalog;
    public long Id { get; } = id;
}

public sealed class DuplicateCatalogCodeException(string catalog, string code)
    : Exception($"El código '{code}' ya está registrado en {catalog}.")
{
    public string Catalog { get; } = catalog;
    public string Code { get; } = code;
}
