using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RegistroConsultas.Api.Filters;

public sealed class AntiforgeryValidationFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS", "TRACE"
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (SafeMethods.Contains(context.HttpContext.Request.Method))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Token antifalsificación inválido o ausente.",
                Detail = "Solicita un token CSRF y vuelve a enviar la operación con la cookie y el header configurado."
            });
        }
    }
}
