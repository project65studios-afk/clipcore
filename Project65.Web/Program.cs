using Project65.Web.Components;
using Microsoft.EntityFrameworkCore;
using Project65.Infrastructure.Data;
using Project65.Core.Interfaces;
using Project65.Infrastructure.Data.Repositories;
using Project65.Infrastructure.Services;
// Ensure Repositories namespace is included, which it is.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<Microsoft.AspNetCore.Identity.IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    });

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IClipRepository, ClipRepository>();
builder.Services.AddScoped<IVideoService, MuxVideoService>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

if (!string.IsNullOrEmpty(builder.Configuration["SendGrid:ApiKey"]))
{
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}

builder.Services.AddScoped<Project65.Web.Services.CartService>();
builder.Services.AddScoped<Project65.Web.Services.CartService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsageRepository, UsageRepository>();
builder.Services.AddMemoryCache();

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
        "default-src 'self' https: data: blob: 'unsafe-inline' 'unsafe-eval'; " +
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

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages(); // Required for Identity UI endpoints

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    await context.Database.MigrateAsync();
    await Project65.Infrastructure.DataSeeder.SeedAsync(context, userManager, roleManager);
}

app.Run();
