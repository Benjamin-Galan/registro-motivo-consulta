using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using RegistroConsultas.Api.Filters;
using RegistroConsultas.Api.Middleware;
using RegistroConsultas.Infrastructure;
using RegistroConsultas.Infrastructure.Identity;
using RegistroConsultas.Infrastructure.Persistence;

var bootstrapAdminRequested = args.Contains("--bootstrap-admin", StringComparer.OrdinalIgnoreCase);
var builderArgs = args.Where(arg => !string.Equals(arg, "--bootstrap-admin", StringComparison.OrdinalIgnoreCase)).ToArray();
var builder = WebApplication.CreateBuilder(builderArgs);

builder.Services.AddControllers(options =>
    options.Filters.AddService<AntiforgeryValidationFilter>());
builder.Services.AddScoped<AntiforgeryValidationFilter>();
builder.Services.AddOpenApi();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "RegistroConsultas.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    var origin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000";
    policy.WithOrigins(origin).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (bootstrapAdminRequested)
{
    await BootstrapAdminAsync(app.Services);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<CatalogExceptionMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task BootstrapAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<RegistroDbContext>();
    await database.Database.MigrateAsync();

    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await users.Users.AnyAsync())
    {
        Console.Error.WriteLine("Ya existen usuarios. No se creó otro administrador.");
        Environment.ExitCode = 1;
        return;
    }

    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Administrador", "Admisionista", "Medico" })
    {
        if (!await roles.RoleExistsAsync(role))
        {
            var result = await roles.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    Console.Write("Nombre de usuario del administrador: ");
    var username = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(username))
    {
        throw new InvalidOperationException("El nombre de usuario es obligatorio.");
    }

    Console.Write("Contraseña (mínimo 12 caracteres, mayúscula, minúscula, número y símbolo): ");
    var password = ReadHiddenPassword();
    Console.WriteLine();

    var admin = new ApplicationUser { UserName = username };
    var createUser = await users.CreateAsync(admin, password);
    if (!createUser.Succeeded)
    {
        throw new InvalidOperationException(string.Join("; ", createUser.Errors.Select(error => error.Description)));
    }

    var assignRole = await users.AddToRoleAsync(admin, "Administrador");
    if (!assignRole.Succeeded)
    {
        await users.DeleteAsync(admin);
        throw new InvalidOperationException(string.Join("; ", assignRole.Errors.Select(error => error.Description)));
    }

    Console.WriteLine($"Administrador '{username}' creado. Guarda sus credenciales de forma segura.");
}

static string ReadHiddenPassword()
{
    var password = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }

    return password.ToString();
}
