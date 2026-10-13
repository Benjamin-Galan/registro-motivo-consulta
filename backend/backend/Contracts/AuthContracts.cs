using System.ComponentModel.DataAnnotations;

namespace RegistroConsultas.Api.Contracts;

public sealed record LoginRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Username,
    [Required, StringLength(200, MinimumLength = 1)] string Password);

public sealed record CreateUserRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Username,
    [Required, StringLength(200, MinimumLength = 12)] string Password,
    [Required, StringLength(30)] string Role);

public sealed record CurrentUserResponse(string Username, IReadOnlyCollection<string> Roles);
public sealed record CreateUserResponse(string Username, string Role);
