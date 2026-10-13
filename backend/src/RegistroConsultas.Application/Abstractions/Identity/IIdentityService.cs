namespace RegistroConsultas.Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<bool> SignInAsync(string username, string password, CancellationToken cancellationToken = default);
    Task SignOutAsync();
    Task<UserCreationResult> CreateUserAsync(string username, string password, string role, CancellationToken cancellationToken = default);
}

public sealed record UserCreationResult(bool Succeeded, IReadOnlyCollection<string> Errors);
