using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace RegistroConsultas.Infrastructure.Persistence;

public sealed class RegistroDbContextFactory : IDesignTimeDbContextFactory<RegistroDbContext>
{
    public RegistroDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiProjectPath = Path.Combine(basePath, "backend", "backend");
        if (File.Exists(Path.Combine(apiProjectPath, "appsettings.json")))
        {
            basePath = apiProjectPath;
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiAssembly = Assembly.Load("RegistroConsultas.Api");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets(apiAssembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Registro")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=RegistroConsultas_DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<RegistroDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new RegistroDbContext(options);
    }
}
