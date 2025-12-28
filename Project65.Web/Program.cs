using Project65.Web.Components;
using Microsoft.EntityFrameworkCore;
using Project65.Infrastructure.Data;
using Project65.Core.Interfaces;
using Project65.Infrastructure.Data.Repositories;
using Project65.Infrastructure.Services;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
// Ensure Repositories namespace is included, which it is.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true; // Enable detailed exceptions
    });
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<Microsoft.AspNetCore.Identity.IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
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

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IClipRepository, ClipRepository>();
builder.Services.AddScoped<IVideoService, MuxVideoService>();
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
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

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

builder.Services.AddScoped<Project65.Web.Services.CartService>();
builder.Services.AddSingleton<Project65.Web.Services.VideoHealingService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddControllers(options =>
{
    // API controllers shouldn't validate antiforgery tokens by default
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
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
    // Permissive CSP to allow external services (Google/Facebook/Stripe/Mux) while providing baseline protection
    // Mux player uses blob: for HLS chunks and workers.
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self' http: https: data: blob: 'unsafe-inline' 'unsafe-eval'; " +
        "worker-src 'self' blob:; " + 
        "frame-ancestors 'self';";
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages(); // Required for Identity UI endpoints
app.MapControllers(); // Required for API endpoints

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    
    // Configure CORS for R2
    var storageService = services.GetRequiredService<IStorageService>();
    await storageService.ConfigureCorsAsync();
    
    await context.Database.MigrateAsync();
    await Project65.Infrastructure.DataSeeder.SeedAsync(context, userManager, roleManager);
}

app.Run();
