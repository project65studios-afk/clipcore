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
// Ensure Repositories namespace is included, which it is.


// Force stdout/stderr to flush immediately for App Runner debugging
Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });

Console.Error.WriteLine(">>> DEPLOYMENT START: Process ID " + Environment.ProcessId);

// 60-second Watchdog to force a log flush if we hang
_ = Task.Run(async () => {
    await Task.Delay(TimeSpan.FromSeconds(60));
    Console.Error.WriteLine(">>> CRITICAL WATCHDOG: App hasn't reached app.Run() after 60s. EXPORTING LOGS AND QUITTING.");
    Environment.Exit(99); 
});

try 
{
    Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Builder Init...");
    var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Loading SSM Params...");
    try 
    {
        builder.Configuration.AddSystemsManager("/project65");
        Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: SSM Params Loaded.");
    }
    catch (Exception ssmEx)
    {
        Console.Error.WriteLine(">>> SSM LOAD FAILURE: " + ssmEx.Message);
    }
    
    // Validate required keys after loading from SSM
    ConfigurationValidation.ValidateRequiredKeys(builder.Configuration,
        "ConnectionStrings:DefaultConnection",
        "Mux:TokenId",
        "Mux:TokenSecret",
        "Mux:SigningKeyId",
        "Mux:SigningKeyPrivate",
        "R2:AccountId",
        "R2:AccessKeyId",
        "R2:SecretAccessKey",
        "R2:BucketName",
        "Stripe:SecretKey",
        "OpenAI:ApiKey",
        "AWS:AccessKeyId",
        "AWS:SecretAccessKey",
        "AWS:Region"
    );
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

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContextFactory<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    // Use ONE factory for everything in Prod to avoid concurrency collisions
    builder.Services.AddDbContextFactory<PostgresDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });

    // Provide Scoped PostgresDbContext (for non-Blazor parts or simple injections)
    builder.Services.AddScoped<PostgresDbContext>(p => p.GetRequiredService<IDbContextFactory<PostgresDbContext>>().CreateDbContext());
    
    // Alias Scoped AppDbContext for Identity and base-class injections
    builder.Services.AddScoped<AppDbContext>(p => p.GetRequiredService<PostgresDbContext>());

    // Crucial: Alias the Factory itself so repositories using IDbContextFactory<AppDbContext> 
    // use the SAME underlying singleton factory and connection pool as PostgresDbContext.
    builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(p => 
        new DelegatingDbContextFactory<AppDbContext, PostgresDbContext>(p.GetRequiredService<IDbContextFactory<PostgresDbContext>>()));
}

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<PostgresDbContext>(); // Use concrete type for Identity stores

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

    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
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

if (!string.IsNullOrEmpty(builder.Configuration["AWS:AccessKeyId"]))
{
    // Register as concrete type first to share instance if needed
    builder.Services.AddScoped<AmazonSESEmailService>();
    // Forward interfaces
    builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<AmazonSESEmailService>());
    builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(sp => sp.GetRequiredService<AmazonSESEmailService>());
}
else
{
    builder.Services.AddScoped<ConsoleEmailService>();
    builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<ConsoleEmailService>());
    builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(sp => sp.GetRequiredService<ConsoleEmailService>());
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

var app = builder.Build();

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
    
    string csp = "default-src 'self'; " +
                 "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://releases.transloadit.com https://js.stripe.com https://www.gstatic.com http://www.gstatic.com https://maps.googleapis.com chrome-extension:; " +
                 "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://releases.transloadit.com https://fonts.googleapis.com https://fonts.gstatic.com; " +
                 "img-src 'self' data: blob: https://*.mux.com https://*.r2.cloudflarestorage.com https://*.stripe.com https://www.gstatic.com http://www.gstatic.com; " +
                 $"connect-src 'self' {appOrigins} https://*.mux.com https://*.r2.cloudflarestorage.com https://api.stripe.com https://www.gstatic.com http://www.gstatic.com https://maps.googleapis.com https://cdn.jsdelivr.net https://unpkg.com https://releases.transloadit.com wss://localhost:* ws://localhost:* chrome-extension:; " +
                 "frame-src 'self' https://js.stripe.com; " +
                 "media-src 'self' blob: https://*.mux.com; " +
                 "worker-src 'self' blob:; " +
                 "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
                 "frame-ancestors 'self';";

    context.Response.Headers["Content-Security-Policy"] = csp;
    
    // Antiforgery Token Cookie for JS (XHR/Fetch)
    // var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
    // var tokens = antiforgery.GetAndStoreTokens(context);
    // context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, 
    //    new CookieOptions { HttpOnly = false, Secure = true, SameSite = SameSiteMode.Strict }); 
    //     new CookieOptions { 
    //         HttpOnly = false, // Must be accessible by JS
    //         Secure = !builder.Environment.IsDevelopment(), 
    //         SameSite = SameSiteMode.Strict 
    //     });

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
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages().RequireRateLimiting("login"); // Required for Identity UI endpoints
app.MapControllers(); // Required for API endpoints
app.MapHub<Project65.Web.Hubs.ProcessingHub>("/processingHub");

Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Starting Post-Build Tasks (Migrations/Seeding)...");
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    
    // Configure CORS for R2
    var storageService = services.GetRequiredService<IStorageService>();
    await storageService.ConfigureCorsAsync();
    
    Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Testing RDS Connection...");
    try 
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await context.Database.CanConnectAsync(cts.Token);
        Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: RDS Connected Successfully.");
    }
    catch (Exception dbEx)
    {
        Console.Error.WriteLine(">>> DATABASE CONNECTION FAILED: " + dbEx.Message);
        throw; // Let the outer catch handle and exit
    }

    Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Database Migrations...");
    await context.Database.MigrateAsync();
    Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Data Seeding...");
    await Project65.Infrastructure.DataSeeder.SeedAsync(context, userManager, roleManager, app.Configuration, app.Environment.IsDevelopment());
    Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Startup Sequence Success.");
}

Console.Error.WriteLine(">>> DEPLOYMENT DEBUG: Kestrel app.Run()...");
    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine("CRITICAL STARTUP FAILURE:");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex.StackTrace);
    throw; // Still throw to ensure non-zero exit code
}

// Simple wrapper to allow IDbContextFactory<Base> to use IDbContextFactory<Derived>
// This ensures we only have ONE singleton factory in the entire application scope.
public class DelegatingDbContextFactory<TBase, TDerived>(IDbContextFactory<TDerived> inner) : IDbContextFactory<TBase>
    where TBase : DbContext
    where TDerived : TBase
{
    public TBase CreateDbContext() => inner.CreateDbContext();
}
