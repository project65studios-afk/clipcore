using Project65.Web.Components;
using Project65.Web.Services;
using Microsoft.EntityFrameworkCore;
using Project65.Infrastructure.Data;
using Project65.Core.Interfaces;
using Project65.Infrastructure.Data.Repositories;
using Project65.Infrastructure.Repositories;
using Project65.Infrastructure.Services;
using Project65.Infrastructure.Services.Fakes;
using Project65.Core.Entities;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
// Ensure Repositories namespace is included, which it is.


// Force stdout/stderr to flush immediately for App Runner debugging
Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });

Console.Error.WriteLine(">>> DEPLOYMENT START: Process ID " + Environment.ProcessId);




Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Builder Init...");
var builder = WebApplication.CreateBuilder(args);

// Flag to track if critical config loaded successfully
bool configLoaded = true;
string configError = "";

// ALWAYS try to load SSM, but handle failures gracefully in Development
Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Starting SSM Pre-Check...");
try
{
    // MANUAL PRE-CHECK: Creates a temporary client with strict timeout.
    // This ensures meaningful connectivity before we let the standard config source 
    // silently hang line 385 (builder.Build).

    var checkTask = Task.Run(async () =>
    {
        var ssmConfig = new AmazonSimpleSystemsManagementConfig
        {
            Timeout = TimeSpan.FromSeconds(5),
            MaxErrorRetry = 0,
            RegionEndpoint = Amazon.RegionEndpoint.USEast1 // Explicit region is safer
        };

        using var ssmClient = new AmazonSimpleSystemsManagementClient(ssmConfig);

        // Just try to list parameters. Light op.
        var req = new GetParametersByPathRequest
        {
            Path = "/project65",
            MaxResults = 1
        };

        await ssmClient.GetParametersByPathAsync(req);
    });

    if (checkTask.Wait(TimeSpan.FromSeconds(6))) // 1s buffer over 5s timeout
    {
        if (checkTask.IsFaulted) throw checkTask.Exception!.InnerException!;

        Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Pre-Check PASSED. Registering Source.");

        // Only now do we register the standard source.
        // It will re-fetch during builder.Build(), but we know the path is open.
        builder.Configuration.AddSystemsManager("/project65");
    }
    else
    {
        throw new TimeoutException("SSM Pre-Check timed out (Network Black Hole).");
    }
}
catch (Exception ssmEx)
{
    // In Production, config failure is fatal/degraded. In Dev, we fall back to local settings.
    if (!builder.Environment.IsDevelopment())
    {
        configLoaded = false;
        configError = $"SSM Pre-Check Failed: {ssmEx.Message}";
        Console.Error.WriteLine($">>> SSM FAILURE (DEGRADED MODE STARTING): {ssmEx.Message}");
    }
    else
    {
        Console.Error.WriteLine($">>> DEV SSM FAILURE (Ignoring): {ssmEx.Message}");
        // configLoaded stays true for Dev
    }
}

// Configure Logging for CloudWatch
builder.Logging.ClearProviders(); // Start fresh
if (builder.Environment.IsDevelopment())
{
    // Pretty text for humans
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}
else
{
    // Structured JSON for machines (CloudWatch)
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = false;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
        {
            Indented = false // Compact JSON
        };
    });
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true; // FORCE ENABLE for App Runner debugging
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSignalR();

// Configure ForwardedHeaders for App Runner (Envoy)
// This fixes the "WebSocket failed to connect" error by ensuring HTTPS is detected correctly.
// Configure ForwardedHeaders for App Runner (Envoy)
// Refined to only trust Proto and For to avoid Host header confusion
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database Configuration
string connectionString = "";
if (configLoaded)
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    // Note: We used ! because configLoaded implies config validation passed, so key exists.
}

// ---------------------------
// DA T A B A S E   S E T U P
// ---------------------------
if (configLoaded)
{
    if (builder.Environment.IsDevelopment())
    {
        // DEVELOPMENT: Use SQLite but maintain the SAME factory structure as Production
        // so that SettingsRepository (which asks for IDbContextFactory<PostgresDbContext>) works.
        builder.Services.AddDbContextFactory<PostgresDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Note: We use PostgresDbContext class, but configured with SQLite options.
        // This is safe because PostgresDbContext is just AppDbContext + strict constructor.

        // Provide Scoped PostgresDbContext
        builder.Services.AddScoped<PostgresDbContext>(p => p.GetRequiredService<IDbContextFactory<PostgresDbContext>>().CreateDbContext());

        // Alias Scoped AppDbContext -> PostgresDbContext
        builder.Services.AddScoped<AppDbContext>(p => p.GetRequiredService<PostgresDbContext>());

        // IMPORTANT: Forward IDbContextFactory<AppDbContext> to the PostgresDbContext factory.
        builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<PostgresDbContext>>();
            return new Project65.Infrastructure.Data.DbContextFactoryWrapper(factory);
        });
    }
    else
    {
        // Runtime Factory: Uses PostgresDbContext (Npgsql) as the SINGLE source of truth
        builder.Services.AddDbContextFactory<PostgresDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Provide Scoped PostgresDbContext (standard resolution)
        builder.Services.AddScoped<PostgresDbContext>(p => p.GetRequiredService<IDbContextFactory<PostgresDbContext>>().CreateDbContext());

        // Alias Scoped AppDbContext -> PostgresDbContext
        builder.Services.AddScoped<AppDbContext>(p => p.GetRequiredService<PostgresDbContext>());

        // IMPORTANT: Forward IDbContextFactory<AppDbContext> to the PostgresDbContext factory.
        builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<PostgresDbContext>>();
            return new Project65.Infrastructure.Data.DbContextFactoryWrapper(factory);
        });
    }

    builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
        .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
        .AddEntityFrameworkStores<PostgresDbContext>();
}
else
{
    Console.Error.WriteLine(">>> SKIPPING DB SETUP: Config load failed.");
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(2); // Shorter expiration for security
    options.SlidingExpiration = true;
});

var authBuilder = builder.Services.AddAuthentication();

// Support both standard and flattened config keys for Google Auth
var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? builder.Configuration["Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId))
{
    var maskedId = googleClientId.Length > 10 ? googleClientId.Substring(0, 10) + "..." : "SHORT_ID";
    Console.WriteLine($">>> GOOGLE AUTH SETUP: Found ClientID starting with '{maskedId}'");
}
else
{
    Console.WriteLine(">>> GOOGLE AUTH SETUP: No ClientID found.");
}

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
    });
}

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IClipRepository, ClipRepository>();
builder.Services.AddSingleton<ISearchService, LevenshteinSearchService>();
builder.Services.AddSingleton<GlobalSettingsNotifier>();
builder.Services.AddScoped<EmailTemplateService>();

if (builder.Environment.IsEnvironment("Testing") || builder.Configuration["USE_FAKE_VIDEO"] == "true")
{
    builder.Services.AddScoped<IVideoService, FakeVideoService>();
}
else
{
    builder.Services.AddScoped<IVideoService, MuxVideoService>();
}

builder.Services.AddScoped<IVisionService, OpenAIVisionService>();
builder.Services.AddHttpClient<OpenAIVisionService>(); // Best practice for HttpClient injection

// Configure R2 manually to ensure ForcePathStyle and correct ServiceURL
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var r2Config = new AmazonS3Config
    {
        ServiceURL = $"https://{builder.Configuration["R2:AccountId"]}.r2.cloudflarestorage.com",
        ForcePathStyle = true,
        // Use AuthenticationRegion to force SigV4 region without overriding the endpoint
        AuthenticationRegion = "us-east-1"
    };

    var creds = new Amazon.Runtime.BasicAWSCredentials(
        builder.Configuration["R2:AccessKeyId"],
        builder.Configuration["R2:SecretAccessKey"]);

    return new AmazonS3Client(creds, r2Config);
});
builder.Services.AddScoped<IStorageService, R2StorageService>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IExternalProductRepository, ExternalProductRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
if (builder.Environment.IsEnvironment("Testing") || builder.Configuration["USE_FAKE_VIDEO"] == "true")
{
    builder.Services.AddScoped<IPaymentService, FakePaymentService>();
}
else
{
    builder.Services.AddScoped<IPaymentService, StripePaymentService>();
}

// Email Service Configuration
var resendApiKey = builder.Configuration["Resend:ApiKey"] ?? builder.Configuration["Resend:ApiToken"];

if (!string.IsNullOrEmpty(resendApiKey))
{
    // Register Resend
    builder.Services.AddOptions();
    builder.Services.AddHttpClient<Resend.ResendClient>();
    builder.Services.Configure<Resend.ResendClientOptions>(o =>
    {
        o.ApiToken = resendApiKey;
    });
    builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();

    builder.Services.AddScoped<ResendEmailService>();
    builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<ResendEmailService>());
    builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(sp => sp.GetRequiredService<ResendEmailService>());

    Console.WriteLine(">>> EMAIL SETUP: Using Resend.");
}

else
{
    builder.Services.AddScoped<ConsoleEmailService>();
    builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<ConsoleEmailService>());
    builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(sp => sp.GetRequiredService<ConsoleEmailService>());

    Console.WriteLine(">>> EMAIL SETUP: Using Console (Dev Mode).");
}

builder.Services.AddScoped<Project65.Web.Services.CartService>();
builder.Services.AddScoped<Project65.Web.Services.StoreSettingsService>();
builder.Services.AddScoped<Project65.Web.Services.SummaryGenerationService>();
builder.Services.AddScoped<IInvoiceService, QuestPdfInvoiceService>();
builder.Services.AddScoped<Project65.Web.Services.OrderFulfillmentService>();
builder.Services.AddSingleton<Project65.Web.Services.VideoHealingService>();

// Configure QuestPDFLicense
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddMemoryCache();
// Configure Antiforgery to use a custom header for API calls
// builder.Services.AddAntiforgery(options => 
// {
//     options.HeaderName = "X-XSRF-TOKEN";
// });

// Implementation of Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var isTestOrDev = builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Testing";

        // Stricter limit for delivery page to prevent session ID enumeration
        // 10 requests per minute per IP
        if (httpContext.Request.Path.StartsWithSegments("/delivery"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon_delivery",
               factory: _ => new FixedWindowRateLimiterOptions
               {
                   AutoReplenishment = true,
                   PermitLimit = 60,
                   QueueLimit = 0,
                   Window = TimeSpan.FromMinutes(1)
               });
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = isTestOrDev ? 1000 : 100,
                QueueLimit = isTestOrDev ? 100 : 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Specific limit for Login
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Specific limit for Admin/Upload actions
    options.AddFixedWindowLimiter("admin", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream", "application/javascript", "text/css" });
});

builder.Services.AddControllers();

// Configure CORS for Mux/R2/Stripe interactions
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5094", "http://127.0.0.1:5094", "https://localhost:7192" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Kestrel to accept large file uploads (up to 2GB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2_147_483_648; // 2GB
});

// Configure form options for large multipart uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2_147_483_648; // 2GB
});

// Configure Kestrel to listen on 0.0.0.0:[PORT] (only in Production/Container)
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Register Startup Background Service
if (configLoaded)
{
    builder.Services.AddHostedService<StartupBackgroundService>();
}

var app = builder.Build();

// Generic Health Check Endpoint
app.MapGet("/health", () => Results.Ok("ok"));

// Enable Forwarded Headers Middleware (Must be early in the pipeline)
app.UseForwardedHeaders();

// Explicitly enable WebSockets REMOVED (Forcing LongPolling)
app.UseWebSockets();

// DEBUG LOGGING REMOVED to reduce noise
// app.Use(async (context, next) => ...);

// DEBUG: View Config Load Status
app.MapGet("/debug/config", () =>
{
    if (configLoaded) return Results.Ok("Config Loaded Successfully.");
    return Results.Problem($"Config Validation Failed: {configError}");
});

// Add Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Hardened CSP: specific allowed origins for Mux, Stripe, and Cloudflare R2
    // mux.com: player/streaming | cloudflarestorage.com: R2 | stripe.com: payments | transloadit.com: uppy
    // We add our own allowed origins to the CSP connect-src
    string appOrigins = string.Join(" ", allowedOrigins);

    // Note: Replaced "ws://localhost:*" with "wss://" and "ws://" dynamically if needed, 
    // but simplified heavily here to ensure it doesn't block local dev.
    string csp = "upgrade-insecure-requests; " + 
                 "default-src 'self'; " +
                 "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://releases.transloadit.com https://js.stripe.com https://www.gstatic.com http://www.gstatic.com https://maps.googleapis.com chrome-extension:; " +
                 "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://releases.transloadit.com https://fonts.googleapis.com https://fonts.gstatic.com; " +
                 "img-src 'self' data: blob: https://*.mux.com https://*.r2.cloudflarestorage.com https://*.stripe.com https://www.gstatic.com http://www.gstatic.com; " +
                 $"connect-src 'self' {appOrigins} https://*.mux.com https://*.r2.cloudflarestorage.com https://api.stripe.com https://www.gstatic.com http://www.gstatic.com https://maps.googleapis.com https://cdn.jsdelivr.net https://unpkg.com https://releases.transloadit.com wss://* ws://* chrome-extension:; " +
                 "frame-src 'self' https://js.stripe.com; " +
                 "media-src 'self' blob: https://*.mux.com; " +
                 "worker-src 'self' blob:; " +
                 "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
                 "frame-ancestors 'self';";

    context.Response.Headers["Content-Security-Policy"] = csp;

    await next();
});

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseDeveloperExceptionPage(); // FORCE ENABLE to print stack trace to logs for v6 debugging

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseCors("ProductionOrigins");
// app.UseRateLimiter();
if (configLoaded)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();


app.MapStaticAssets();

if (configLoaded)
{
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // REMOVED: Explicit MapBlazorHub config. 
    // We rely on the CLIENT (App.razor) to request LongPolling.
    // Forcing it here caused a 500 error on /_blazor/initializers.

    app.MapRazorPages().RequireRateLimiting("login"); // Required for Identity UI endpoints
    app.MapControllers(); // Required for API endpoints
    app.MapHub<Project65.Web.Hubs.ProcessingHub>("/processingHub");
}
else
{
    // In degraded mode, we map a fallback homepage or just let the static assets/debug endpoints work.
    // If we map App.razor without services, it will crash on injection.
    app.MapGet("/", () => Results.Redirect("/debug/config"));
}

// REMOVED: Blocking database migration block. 
// This is now handled by StartupBackgroundService.cs to ensure port 8080 opens immediately.

Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Kestrel app.Run()...");

// BLOCKING STARTUP TASK: Configure CORS for R2 synchronously
// This guarantees rules are applied BEFORE any user request can hit the server,
// solving the "first load broken image" race condition.
if (configLoaded)
{
    try 
    {
        Console.Error.WriteLine(">>> STARTUP: Applying R2 CORS Rules...");
        using (var scope = app.Services.CreateScope())
        {
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            // We wait for this to finish before allowing the app to start listening
            storageService.ConfigureCorsAsync().GetAwaiter().GetResult();
            Console.Error.WriteLine(">>> STARTUP: R2 CORS Rules Applied Successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($">>> STARTUP ERROR: Failed to apply R2 CORS rules: {ex.Message}");
    }

    // BLOCKING STARTUP TASK: Migrate Data
    try 
    {
        Console.Error.WriteLine(">>> STARTUP: Running Data Migrations...");
        using (var scope = app.Services.CreateScope())
        {
            var purchaseRepo = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
            // Update existing GIF purchases to new LicenseType enum value to prevent collision
            purchaseRepo.MigrateGifLicenseTypesAsync().GetAwaiter().GetResult();
            Console.Error.WriteLine(">>> STARTUP: Data Migrations Complete.");
        }
    }
    catch (Exception ex)
    {
         Console.Error.WriteLine($">>> STARTUP ERROR: Data Migration Failed: {ex.Message}");
    }
}

app.Run();
