using System.Text;
using ClipCore.API.Data;
using ClipCore.API.Identity;
using ClipCore.API.Interfaces;
using ClipCore.API.Services;
using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── NO snake_case mapping ─────────────────────────────────────────────────────
// Tables/columns are PascalCase. Do NOT add DefaultTypeMap.MatchNamesWithUnderscores.
// Confirmed by: DELETE FROM "Purchases" in PurchaseRepository raw SQL.

// ── Identity (thin EF context — Identity tables only) ────────────────────────
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppIdentityDbContext>()
.AddDefaultTokenProviders();

// ── JWT ───────────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer           = false,
        ValidateAudience         = false,
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── CORS — AllowedOrigins from config, not AllowAll ───────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
    options.AddPolicy("ConfiguredOrigins", p =>
        p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

// ── Dapper data access ────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISqlDataAccess,   SqlDataAccess>();
builder.Services.AddSingleton<ISellerData,      SellerData>();
builder.Services.AddSingleton<IClipData,         ClipData>();
builder.Services.AddSingleton<ICollectionData,   CollectionData>();
builder.Services.AddSingleton<IPurchaseData,     PurchaseData>();
builder.Services.AddSingleton<IMarketplaceData,  MarketplaceData>();
builder.Services.AddSingleton<IAdminData,        AdminData>();
builder.Services.AddSingleton<IPromoCodeData,    PromoCodeData>();
builder.Services.AddSingleton<ISettingsData,     SettingsData>();
builder.Services.AddSingleton<IUsageData,        UsageData>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenService,             TokenService>();
builder.Services.AddScoped<IMuxService,               MuxService>();
builder.Services.AddScoped<IR2StorageService,          R2StorageService>();
builder.Services.AddScoped<IOrderFulfillmentService,  OrderFulfillmentService>();
builder.Services.AddScoped<IEmailService,             SesEmailService>();
builder.Services.AddHostedService<ClipArchiveService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClipCore API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        Array.Empty<string>()
    }});
});

var app = builder.Build();

// ── Seed roles and admin user on startup ──────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DataSeeder.SeedAsync(userManager, roleManager, builder.Configuration);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("ConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
