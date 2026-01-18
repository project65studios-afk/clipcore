using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Project65.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Project65.Core.Entities;
using Project65.Web.Services;
using Microsoft.Extensions.Configuration; // For IConfiguration
using Project65.Core.Interfaces;

namespace Project65.Web.Services;

public sealed class StartupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public StartupBackgroundService(
        IServiceProvider serviceProvider, 
        ILogger<StartupBackgroundService> logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait a few seconds to ensure Kestrel is fully up (optional but safe)
            await Task.Delay(2000, stoppingToken);

            _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Beginning database initialization...");
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            // Use PostgresDbContext for migrations
            var context = services.GetRequiredService<PostgresDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var storageService = services.GetRequiredService<IStorageService>();

            // Configure CORS for R2 - usually quick, but good to have here
            await storageService.ConfigureCorsAsync();

            _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Testing connection...");
            try 
            {
                var connString = context.Database.GetConnectionString();
                var host = connString?.Split(';').FirstOrDefault(s => s.StartsWith("Host=")) ?? "Unknown";
                _logger.LogInformation($">>> STARTUP BACKGROUND SERVICE: Target DB Host: {host}");

                _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Checking for pending migrations...");
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(stoppingToken);
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($">>> STARTUP BACKGROUND SERVICE: Found {pendingMigrations.Count()} pending migrations: {string.Join(", ", pendingMigrations)}");
                    _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Running MigrateAsync...");
                    await context.Database.MigrateAsync(stoppingToken);
                    _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Migrations Applied.");
                }
                else
                {
                    _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: No pending migrations found. Database is up to date.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> STARTUP BACKGROUND SERVICE: Data Initialization Failed.");
            }

            _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Running Seeder...");
            await Project65.Infrastructure.DataSeeder.SeedAsync(context, userManager, roleManager, _configuration, _environment.IsDevelopment());

            _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Initialization Completed Successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(">>> STARTUP BACKGROUND SERVICE: Operation Canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> STARTUP BACKGROUND SERVICE: CRITICAL FAILURE.");
        }
    }
}
