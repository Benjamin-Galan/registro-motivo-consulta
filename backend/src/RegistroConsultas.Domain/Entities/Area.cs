namespace RegistroConsultas.Domain.Entities;

public sealed class Area
{
    private Area() { }

    public Area(string codigoArea, string nombre, DateTimeOffset ahora)
    {
        CambiarDatos(codigoArea, nombre, ahora);
        CreadoEl = ahora;
    }

    public long Id { get; private set; }
    public string CodigoArea { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public bool RegAnulado { get; private set; }
    public DateTimeOffset CreadoEl { get; private set; }
    public DateTimeOffset? ModificadoEl { get; private set; }

    public void CambiarDatos(string codigoArea, string nombre, DateTimeOffset ahora)
    {
        CodigoArea = NormalizarCodigo(codigoArea);
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
