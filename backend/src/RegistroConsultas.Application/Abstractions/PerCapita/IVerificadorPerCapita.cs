namespace RegistroConsultas.Application.Abstractions.PerCapita;

/// <summary>
/// Consulta la fuente institucional de per cápita. Un fallo técnico nunca debe
/// interpretarse como una vigencia confirmada.
/// </summary>
public interface IVerificadorPerCapita
{
    Task<ResultadoPerCapita> VerificarAsync(
        string identificador,
        CancellationToken cancellationToken = default);
}

public sealed record ResultadoPerCapita(
    bool Activo,
    DateTimeOffset VerificadoEn,
    string? CodigoTipoAsegurado = null);
