using Microsoft.AspNetCore.Mvc;
using RegistroConsultas.Domain.Exceptions;

namespace RegistroConsultas.Api.Middleware;

public sealed class CatalogExceptionMiddleware(RequestDelegate next, ILogger<CatalogExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (CatalogItemNotFoundException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Recurso no encontrado.", exception.Message);
        }
        catch (DuplicateCatalogCodeException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Código duplicado.", exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Solicitud inválida.", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error inesperado en la API. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado.", "Consulta el identificador de seguimiento con soporte.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        });
    }
}
