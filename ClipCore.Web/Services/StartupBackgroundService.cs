using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ClipCore.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using ClipCore.Core.Entities;
using ClipCore.Web.Services;
using Microsoft.Extensions.Configuration; // For IConfiguration
using ClipCore.Core.Interfaces;

namespace ClipCore.Web.Services;

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
            // Removed Task.Delay to ensure CORS applies immediately
            _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: Beginning database initialization...");
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            // Use PostgresDbContext for migrations
            var context = services.GetRequiredService<PostgresDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var storageService = services.GetRequiredService<IStorageService>();

            // Double-Tap CORS Configuration
            // We do this Sychronously in Program.cs AND here asynchronously to be absolutely sure.
            // This catches cases where Program.cs might have timed out or failed silently.
            _ = Task.Run(async () => {
                try {
                     await Task.Delay(2000); // Short delay to let things settle
                     await storageService.ConfigureCorsAsync();
                     _logger.LogInformation(">>> STARTUP BACKGROUND SERVICE: R2 CORS policies re-applied (Async Backup).");
                } catch (Exception ex) {
                     _logger.LogError(ex, ">>> STARTUP BACKGROUND SERVICE: Async CORS Backup Failed.");
                }
            });



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
            await ClipCore.Infrastructure.DataSeeder.SeedAsync(context, userManager, roleManager, _configuration, _environment.IsDevelopment());

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
