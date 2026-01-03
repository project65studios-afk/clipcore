using ClipCore.Web.Components;
using ClipCore.Web.Services;
using Microsoft.EntityFrameworkCore;
using ClipCore.Infrastructure.Data;
using ClipCore.Core.Interfaces;
using ClipCore.Infrastructure.Data.Repositories;
using ClipCore.Infrastructure.Repositories;
using ClipCore.Infrastructure.Services;
using ClipCore.Infrastructure.Services.Fakes;
using ClipCore.Core.Entities;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
// Ensure Repositories namespace is included, which it is.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment(); // Only enable detailed exceptions in Dev
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

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

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? string.Empty;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? string.Empty;
    });

// Add Tenant Context (Scoped, so it lives for the request)
builder.Services.AddScoped<ClipCore.Infrastructure.Services.TenantContext>();
// Register it as ITenantProvider so AppDbContext can use it
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<ClipCore.Infrastructure.Services.TenantContext>());

builder.Services.AddScoped<IEventRepository, EventRepository>();
// builder.Services.AddScoped<ITenantProvider, FakeTenantProvider>(); // REMOVED
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

// Configure R2 (via AWS SDK)
var r2Options = builder.Configuration.GetAWSOptions();
r2Options.Credentials = new Amazon.Runtime.BasicAWSCredentials(
    builder.Configuration["R2:AccessKeyId"], 
    builder.Configuration["R2:SecretAccessKey"]);
r2Options.Region = Amazon.RegionEndpoint.USEast1; // R2 requires a region, usually ignored or auto
r2Options.DefaultClientConfig.ServiceURL = $"https://{builder.Configuration["R2:AccountId"]}.r2.cloudflarestorage.com";

builder.Services.AddDefaultAWSOptions(r2Options);
builder.Services.AddAWSService<IAmazonS3>();
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
    builder.Services.AddScoped<IEmailService, AmazonSESEmailService>();
}
else if (!string.IsNullOrEmpty(builder.Configuration["SendGrid:ApiKey"]))
{
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}

builder.Services.AddScoped<ClipCore.Web.Services.CartService>();
builder.Services.AddScoped<ClipCore.Web.Services.StoreSettingsService>();
builder.Services.AddScoped<ClipCore.Web.Services.SummaryGenerationService>();
builder.Services.AddScoped<IInvoiceService, QuestPdfInvoiceService>();
builder.Services.AddSingleton<ClipCore.Web.Services.VideoHealingService>();

// Configure QuestPDFLicense
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddMemoryCache();
// Configure Antiforgery to use a custom header for API calls
builder.Services.AddAntiforgery(options => 
{
    options.HeaderName = "X-XSRF-TOKEN";
});

// Implementation of Rate Limiting
builder.Services.AddRateLimiter(options => 
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global rate limit
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var isTestOrDev = builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Testing";
        
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
                 "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://releases.transloadit.com https://js.stripe.com https://www.gstatic.com http://www.gstatic.com chrome-extension:; " +
                 "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://releases.transloadit.com https://fonts.googleapis.com; " +
                 "img-src 'self' data: blob: https://*.mux.com https://*.r2.cloudflarestorage.com https://*.stripe.com https://www.gstatic.com http://www.gstatic.com; " +
                 $"connect-src 'self' {appOrigins} https://*.mux.com https://*.r2.cloudflarestorage.com https://api.stripe.com https://www.gstatic.com http://www.gstatic.com https://cdn.jsdelivr.net https://unpkg.com https://releases.transloadit.com wss://localhost:* ws://localhost:* chrome-extension:; " +
                 "frame-src 'self' https://js.stripe.com; " +
                 "media-src 'self' blob: https://*.mux.com; " +
                 "worker-src 'self' blob:; " +
                 "font-src 'self' https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
                 "frame-ancestors 'self';";

    context.Response.Headers["Content-Security-Policy"] = csp;
    
    // Antiforgery Token Cookie for JS (XHR/Fetch)
    var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, 
        new CookieOptions { 
            HttpOnly = false, // Must be accessible by JS
            Secure = !builder.Environment.IsDevelopment(), 
            SameSite = SameSiteMode.Strict 
        });

    await next();
});

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseCors("ProductionOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseMiddleware<ClipCore.Web.Middleware.TenantResolutionMiddleware>();


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages().RequireRateLimiting("login"); // Required for Identity UI endpoints
app.MapControllers(); // Required for API endpoints
app.MapHub<ClipCore.Web.Hubs.ProcessingHub>("/processingHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    
    // Configure CORS for R2
    var storageService = services.GetRequiredService<IStorageService>();
    await storageService.ConfigureCorsAsync();
    
    await context.Database.MigrateAsync();

    // Bootstrap TenantContext for Seeding
    var tenantContext = services.GetRequiredService<ClipCore.Infrastructure.Services.TenantContext>();
    await ClipCore.Infrastructure.DataSeeder.SeedAsync(context, userManager, roleManager, tenantContext);
}

app.Run();
