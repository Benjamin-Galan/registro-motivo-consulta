using Microsoft.AspNetCore.Identity;
using RegistroConsultas.Application.Abstractions.Identity;

namespace RegistroConsultas.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Administrador", "Admisionista", "Medico"
    };

    public async Task<bool> SignInAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var result = await signInManager.PasswordSignInAsync(
            username.Trim(), password, isPersistent: false, lockoutOnFailure: true);

        return result.Succeeded;
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public async Task<UserCreationResult> CreateUserAsync(
        string username,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        role = role.Trim();
        if (!AllowedRoles.Contains(role))
        {
            return new UserCreationResult(false, ["El rol solicitado no está permitido."]);
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            EmailConfirmed = false
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return new UserCreationResult(false, createResult.Errors.Select(error => error.Description).ToArray());
        }

        var roleResult = await userManager.AddToRoleAsync(user, AllowedRoles.Single(item =>
            string.Equals(item, role, StringComparison.OrdinalIgnoreCase)));

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return new UserCreationResult(false, roleResult.Errors.Select(error => error.Description).ToArray());
        }

        return new UserCreationResult(true, []);
    }
}
