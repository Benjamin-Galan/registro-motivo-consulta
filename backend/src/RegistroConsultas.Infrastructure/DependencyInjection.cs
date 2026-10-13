using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegistroConsultas.Application.Abstractions.Identity;
using RegistroConsultas.Application.Abstractions.Catalogs;
using RegistroConsultas.Application.Features.Catalogs;
using RegistroConsultas.Infrastructure.Identity;
using RegistroConsultas.Infrastructure.Persistence;

namespace RegistroConsultas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Registro");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Falta configurar ConnectionStrings:Registro.");
        }

        services.AddDbContext<RegistroDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IZonaFilialRepository, ZonaFilialRepository>();
        services.AddScoped<ICatalogManagementService, CatalogManagementService>();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<RegistroDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "RegistroConsultas.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.None;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }
}
