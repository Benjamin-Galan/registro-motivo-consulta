using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistroConsultas.Api.Contracts;
using RegistroConsultas.Application.Abstractions.Identity;

namespace RegistroConsultas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IIdentityService identityService,
    IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var signedIn = await identityService.SignInAsync(request.Username, request.Password, cancellationToken);
        if (!signedIn)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No fue posible iniciar sesión.",
                Detail = "Verifica el usuario y la contraseña o consulta al administrador."
            });
        }

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await identityService.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var roles = User.Claims
            .Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();

        return Ok(new CurrentUserResponse(User.Identity?.Name ?? string.Empty, roles));
    }
}
