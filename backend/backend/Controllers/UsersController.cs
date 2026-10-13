using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistroConsultas.Api.Contracts;
using RegistroConsultas.Application.Abstractions.Identity;

namespace RegistroConsultas.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Administrador")]
public sealed class UsersController(IIdentityService identityService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.CreateUserAsync(
            request.Username,
            request.Password,
            request.Role,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["user"] = result.Errors.ToArray()
            }));
        }

        return StatusCode(StatusCodes.Status201Created,
            new CreateUserResponse(request.Username.Trim(), request.Role.Trim()));
    }
}
