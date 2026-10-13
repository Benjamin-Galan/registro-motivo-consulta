namespace RegistroConsultas.Domain.Entities;

public sealed class ZonaFilial
{
    private ZonaFilial() { }

    public ZonaFilial(string codigoZona, string nombre, DateTimeOffset ahora)
    {
        CambiarDatos(codigoZona, nombre, ahora);
        CreadoEl = ahora;
    }

    public long Id { get; private set; }
    public string CodigoZona { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public bool RegAnulado { get; private set; }
    public DateTimeOffset CreadoEl { get; private set; }
    public DateTimeOffset? ModificadoEl { get; private set; }

    public void CambiarDatos(string codigoZona, string nombre, DateTimeOffset ahora)
    {
        CodigoZona = NormalizarCodigo(codigoZona);
        Nombre = NormalizarNombre(nombre);
        ModificadoEl = Id == 0 && CreadoEl == default ? null : ahora;
    }

    public void CambiarActivo(bool activo, DateTimeOffset ahora)
    {
        RegAnulado = !activo;
        ModificadoEl = ahora;
    }

    private static string NormalizarCodigo(string value)
    {
        var code = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 20 ||
            code.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("El código es obligatorio, admite hasta 20 letras, números, guiones o guiones bajos.");
        }

        return code;
    }

    private static string NormalizarNombre(string value)
    {
        var name = value?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
        {
            throw new ArgumentException("El nombre es obligatorio y admite hasta 120 caracteres.");
        }

        return name;
    }
}
