# ClipCore.API — Complete Implementation Handoff

This document contains everything needed to build `ClipCore.API` from scratch.
Read the full context section before writing any code.

---

## CONTEXT

**What ClipCore is:** A multi-tenant video clip marketplace SaaS. Videographers (Sellers) upload
clips from events, buyers purchase them through branded storefronts at `/store/{slug}`.
PhotoReflect for video.

**What we're building:** `ClipCore.API` — a new ASP.NET Core Web API project that replaces the
existing `ClipCore.Web` (Blazor Server). The frontend will be Next.js (separate project, not
covered here).

**Existing codebase:**
- `ClipCore.Core` — domain entities, keep untouched
- `ClipCore.Infrastructure` — EF Core repositories, **being replaced by Dapper**
- `ClipCore.Web` — Blazor Server, **being replaced by this API**

**GitHub:** `https://github.com/project65studios-afk/clipcore` (public)

---

## ARCHITECTURE DECISIONS

| Decision | Choice | Reason |
|---|---|---|
| Data access | Dapper + PostgreSQL functions | IATSE111 pattern, explicit SQL |
| Auth | ASP.NET Identity (thin EF) + JWT | Identity tables stay EF, business data → Dapper |
| Email | AWS SES | Existing `AWS` config block in appsettings |
| Video uploads | @mux/upchunk (React) | Already in codebase |
| Image uploads | react-dropzone + presigned R2 URL | Lightweight, headless |
| CORS | AllowedOrigins from config | Not AllowAll |
| PostgreSQL naming | **PascalCase** tables and columns | No `UseSnakeCaseNamingConvention()` in AppDbContext |

**CRITICAL — PostgreSQL schema is PascalCase.**
`AppDbContext` has no `UseSnakeCaseNamingConvention()`. Confirmed by raw SQL in existing repo:
`DELETE FROM "Purchases"`. Every table and column must be quoted PascalCase in all SQL.
Do NOT add `DefaultTypeMap.MatchNamesWithUnderscores = true`.

---

## EXISTING ENTITIES (ClipCore.Core — do not modify)

```csharp
// Clip.cs
public class Clip {
    public string Id { get; set; } = Guid.NewGuid().ToString();  // TEXT PK
    public string CollectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public int PriceCommercialCents { get; set; }
    public bool AllowGifSale { get; set; } = false;
    public int GifPriceCents { get; set; } = 199;
    public string PlaybackIdSigned { get; set; } = string.Empty;
    public string? PlaybackIdTeaser { get; set; }
    public double? DurationSec { get; set; }
    public DateTime? RecordingStartedAt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string TagsJson { get; set; } = "[]";
    public string? MuxUploadId { get; set; }
    public string? MuxAssetId { get; set; }
    public string? MasterFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public bool IsDirectUpload { get; set; } = false;
    public int? SellerId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public DateTime? LastSoldAt { get; set; }
}

// Collection.cs
public class Collection {
    public string Id { get; set; } = Guid.NewGuid().ToString();  // TEXT PK
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? HeroClipId { get; set; }
    public bool DefaultAllowGifSale { get; set; } = false;
    public int DefaultGifPriceCents { get; set; } = 199;
    public int DefaultPriceCents { get; set; } = 1000;
    public int DefaultPriceCommercialCents { get; set; } = 4900;
    public int? SellerId { get; set; }
    public List<Clip> Clips { get; set; } = new();
}

// Seller.cs
public class Seller {
    public int Id { get; set; }        // INT PK
    public string UserId { get; set; } = string.Empty;
    public bool IsTrusted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Storefront.cs
public class Storefront {
    public int Id { get; set; }
    public int SellerId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Subdomain { get; set; }        // Phase 2
    public bool SubdomainActive { get; set; } = false;
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Purchase.cs
public class Purchase {
    public int Id { get; set; }
    public string? UserId { get; set; }           // Nullable = guest checkout
    public string? ClipId { get; set; }
    public string? ClipTitle { get; set; }        // Snapshot
    public string? CollectionName { get; set; }   // Snapshot
    public DateOnly? CollectionDate { get; set; } // Snapshot
    public DateTime? ClipRecordingStartedAt { get; set; }
    public double? ClipDurationSec { get; set; }
    public string? ClipMasterFileName { get; set; }
    public string? ClipThumbnailFileName { get; set; }
    public string StripeSessionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FulfillmentStatus FulfillmentStatus { get; set; } = FulfillmentStatus.Pending;
    public string? HighResDownloadUrl { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? FulfillmentMuxAssetId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    public int? SellerId { get; set; }
    public int PricePaidCents { get; set; }
    public int PlatformFeeCents { get; set; }
    public int SellerPayoutCents { get; set; }
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
    public bool IsGif { get; set; } = false;
    public double? GifStartTime { get; set; }
    public double? GifEndTime { get; set; }
    public string? BrandedPlaybackId { get; set; }
}

// Enums
public enum FulfillmentStatus { Pending, Fulfilled }
public enum LicenseType { Personal, Commercial, Gif }
public enum DiscountType { Percentage, FixedAmount }

// PromoCode.cs
public class PromoCode {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public int Value { get; set; }
    public int? MaxUsages { get; set; }
    public int UsageCount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsValid() { ... }           // Business logic stays on entity
    public long CalculateDiscount(...) { ... }
}

// Setting.cs
public class Setting {
    [Key] public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// DailyWatchUsage.cs — rate limiting for video token requests
public class DailyWatchUsage {
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int TokenRequestCount { get; set; }
}

// ApplicationUser.cs
public class ApplicationUser : IdentityUser {
    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public virtual Seller? Seller { get; set; }
}

// ReservedSlugs.cs (in ClipCore.Core)
public static class ReservedSlugs {
    public static bool IsReserved(string slug) { ... }
}
```

---

## PROJECT FILE — ClipCore.API.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ClipCore.Core\ClipCore.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Dapper"                                          Version="2.1.35" />
    <PackageReference Include="Npgsql"                                          Version="8.0.5" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL"           Version="9.0.4" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer"   Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt"                 Version="8.2.1" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens"                  Version="8.2.1" />
    <PackageReference Include="Stripe.net"                                      Version="46.3.0" />
    <PackageReference Include="Mux.Csharp.Sdk"                                  Version="0.7.0" />
    <PackageReference Include="AWSSDK.S3"                                       Version="3.7.400.0" />
    <PackageReference Include="AWSSDK.SimpleEmail"                              Version="3.7.400.0" />
    <PackageReference Include="Swashbuckle.AspNetCore"                          Version="7.2.0" />
  </ItemGroup>
</Project>
```

---

## Program.cs

```csharp
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("ConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## appsettings.json

```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://clipcore.com"
  ],
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=clipcore;Username=...;Password=..."
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-here"
  },
  "AWS": {
    "AccessKeyId":     "",
    "SecretAccessKey": "",
    "Region":          "us-east-1",
    "FromEmail":       "no-reply@clipcore.com"
  },
  "Mux": {
    "TokenId":       "",
    "TokenSecret":   "",
    "WebhookSecret": ""
  },
  "Stripe": {
    "SecretKey":     "",
    "WebhookSecret": ""
  },
  "R2": {
    "AccountId":       "",
    "AccessKeyId":     "",
    "SecretAccessKey": "",
    "BucketName":      "clipcore-masters"
  }
}
```

---

## Identity/AppIdentityDbContext.cs

```csharp
using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClipCore.API.Identity;

// Thin EF context — manages ONLY AspNetUsers, AspNetRoles, etc.
// All business data (Clips, Sellers, Purchases...) goes through Dapper.
public class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
        : base(options) { }
}
```

---

## Helpers/SqlDataAccess.cs (Dapper wrapper)

```csharp
using System.Data;
using ClipCore.API.Interfaces;
using Dapper;
using Npgsql;

namespace ClipCore.API.Helpers;

public class SqlDataAccess : ISqlDataAccess
{
    private readonly IConfiguration _config;

    public SqlDataAccess(IConfiguration config) => _config = config;

    private IDbConnection CreateConnection() =>
        new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.QueryAsync<T>(sql, parameters);
    }

    public async Task<IEnumerable<T>> LoadData<T>(string sql)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.QueryAsync<T>(sql);
    }

    public async Task<T?> LoadSingle<T, U>(string sql, U parameters)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task SaveData<T>(string sql, T parameters)
    {
        using IDbConnection conn = CreateConnection();
        await conn.ExecuteAsync(sql, parameters);
    }

    public async Task<T?> ExecuteScalar<T, U>(string sql, U parameters)
    {
        using IDbConnection conn = CreateConnection();
        return await conn.ExecuteScalarAsync<T>(sql, parameters);
    }
}
```

## Interfaces/ISqlDataAccess.cs

```csharp
namespace ClipCore.API.Interfaces;

public interface ISqlDataAccess
{
    Task<IEnumerable<T>> LoadData<T, U>(string sql, U parameters);
    Task<IEnumerable<T>> LoadData<T>(string sql);
    Task<T?> LoadSingle<T, U>(string sql, U parameters);
    Task SaveData<T>(string sql, T parameters);
    Task<T?> ExecuteScalar<T, U>(string sql, U parameters);
}
```

---

## PostgreSQL Functions (sql/functions/cc_functions.sql)

Deploy this file to the database before running the API.
Naming convention: `cc_s_` = SELECT, `cc_i_` = INSERT, `cc_u_` = UPDATE, `cc_d_` = DELETE.
Called from Dapper as: `SELECT * FROM cc_s_fn(@param)` or `CALL cc_u_fn(@param)`.

```sql
-- ─────────────────────────────────────────────────────────────
-- SELLERS
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_seller_profile(p_seller_id integer)
RETURNS TABLE (
    "Id" integer, "UserId" text, "IsTrusted" boolean, "CreatedAt" timestamptz,
    "Email" text, "Slug" text, "DisplayName" text, "LogoUrl" text,
    "BannerUrl" text, "AccentColor" text, "Bio" text, "IsPublished" boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT s."Id", s."UserId", s."IsTrusted", s."CreatedAt",
           u."Email",
           sf."Slug", sf."DisplayName", sf."LogoUrl", sf."BannerUrl",
           sf."AccentColor", sf."Bio", sf."IsPublished"
    FROM "Sellers" s
    JOIN "AspNetUsers" u  ON u."Id"        = s."UserId"
    JOIN "Storefronts" sf ON sf."SellerId" = s."Id"
    WHERE s."Id" = p_seller_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_seller_profile_by_user(p_user_id text)
RETURNS TABLE (
    "Id" integer, "UserId" text, "IsTrusted" boolean, "CreatedAt" timestamptz,
    "Email" text, "Slug" text, "DisplayName" text, "LogoUrl" text,
    "BannerUrl" text, "AccentColor" text, "Bio" text, "IsPublished" boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT s."Id", s."UserId", s."IsTrusted", s."CreatedAt",
           u."Email",
           sf."Slug", sf."DisplayName", sf."LogoUrl", sf."BannerUrl",
           sf."AccentColor", sf."Bio", sf."IsPublished"
    FROM "Sellers" s
    JOIN "AspNetUsers" u  ON u."Id"        = s."UserId"
    JOIN "Storefronts" sf ON sf."SellerId" = s."Id"
    WHERE s."UserId" = p_user_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_seller(p_user_id text)
RETURNS integer AS $$
DECLARE v_id integer;
BEGIN
    INSERT INTO "Sellers" ("UserId", "IsTrusted", "CreatedAt")
    VALUES (p_user_id, false, NOW()) RETURNING "Id" INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_i_storefront(p_seller_id integer, p_slug text, p_display_name text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "Storefronts" ("SellerId", "Slug", "DisplayName", "IsPublished", "CreatedAt")
    VALUES (p_seller_id, p_slug, p_display_name, false, NOW());
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_storefront_settings(
    p_seller_id integer, p_display_name text, p_logo_url text,
    p_banner_url text, p_accent_color text, p_bio text, p_is_published boolean
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Storefronts" SET
        "DisplayName" = p_display_name, "LogoUrl" = p_logo_url,
        "BannerUrl" = p_banner_url, "AccentColor" = p_accent_color,
        "Bio" = p_bio, "IsPublished" = p_is_published
    WHERE "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_seller_sales_stats(p_seller_id integer)
RETURNS TABLE (
    "TotalSales" integer, "TotalRevenueCents" bigint,
    "TotalPayoutCents" bigint, "PendingFulfillment" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT COUNT(*)::integer,
           COALESCE(SUM("PricePaidCents"), 0),
           COALESCE(SUM("SellerPayoutCents"), 0),
           COUNT(*) FILTER (WHERE "FulfillmentStatus" = 0)::integer
    FROM "Purchases" WHERE "SellerId" = p_seller_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_slug_exists(p_slug text)
RETURNS boolean AS $$
BEGIN
    RETURN EXISTS (SELECT 1 FROM "Storefronts" WHERE "Slug" = p_slug);
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- CLIPS
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_clips_by_seller(p_seller_id integer)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdTeaser" text,
    "ThumbnailFileName" text, "IsArchived" boolean, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."IsArchived", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."SellerId" = p_seller_id
    ORDER BY c."PublishedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_clips_by_collection(p_collection_id text)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdTeaser" text,
    "ThumbnailFileName" text, "IsArchived" boolean, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."IsArchived", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."CollectionId" = p_collection_id AND c."IsArchived" = false
    ORDER BY c."PublishedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_clip_detail(p_clip_id text)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdSigned" text, "PlaybackIdTeaser" text,
    "MuxAssetId" text, "MuxUploadId" text, "MasterFileName" text,
    "ThumbnailFileName" text, "TagsJson" text, "RecordingStartedAt" timestamptz,
    "Width" integer, "Height" integer, "IsArchived" boolean,
    "ArchivedAt" timestamptz, "LastSoldAt" timestamptz, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdSigned", c."PlaybackIdTeaser",
           c."MuxAssetId", c."MuxUploadId", c."MasterFileName", c."ThumbnailFileName",
           c."TagsJson", c."RecordingStartedAt", c."Width", c."Height",
           c."IsArchived", c."ArchivedAt", c."LastSoldAt", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."Id" = p_clip_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_clips_search(p_query text)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdTeaser" text,
    "ThumbnailFileName" text, "IsArchived" boolean, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."IsArchived", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."IsArchived" = false
      AND (c."Title" ILIKE '%' || p_query || '%' OR c."TagsJson" ILIKE '%' || p_query || '%')
    ORDER BY c."RecordingStartedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_clip(
    p_id text, p_collection_id text, p_seller_id integer, p_title text,
    p_price_cents integer, p_price_commercial_cents integer,
    p_allow_gif_sale boolean, p_gif_price_cents integer, p_tags_json text
) RETURNS void AS $$
BEGIN
    INSERT INTO "Clips"
        ("Id", "CollectionId", "SellerId", "Title", "PriceCents", "PriceCommercialCents",
         "AllowGifSale", "GifPriceCents", "TagsJson", "PlaybackIdSigned", "IsArchived", "PublishedAt")
    VALUES
        (p_id, p_collection_id, p_seller_id, p_title, p_price_cents, p_price_commercial_cents,
         p_allow_gif_sale, p_gif_price_cents, p_tags_json, '', false, NOW());
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_clip(
    p_clip_id text, p_seller_id integer, p_title text,
    p_price_cents integer, p_price_commercial_cents integer,
    p_allow_gif_sale boolean, p_gif_price_cents integer, p_tags_json text
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "Title" = p_title, "PriceCents" = p_price_cents,
        "PriceCommercialCents" = p_price_commercial_cents,
        "AllowGifSale" = p_allow_gif_sale, "GifPriceCents" = p_gif_price_cents,
        "TagsJson" = p_tags_json
    WHERE "Id" = p_clip_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_batch_settings(
    p_collection_id text, p_seller_id integer, p_price_cents integer,
    p_price_commercial_cents integer, p_allow_gif boolean, p_gif_price_cents integer
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "PriceCents" = p_price_cents, "PriceCommercialCents" = p_price_commercial_cents,
        "AllowGifSale" = p_allow_gif, "GifPriceCents" = p_gif_price_cents
    WHERE "CollectionId" = p_collection_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_mux_data(
    p_clip_id text, p_mux_asset_id text, p_playback_signed text,
    p_playback_teaser text, p_duration_sec double precision,
    p_width integer, p_height integer
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "MuxAssetId" = p_mux_asset_id, "PlaybackIdSigned" = p_playback_signed,
        "PlaybackIdTeaser" = p_playback_teaser, "DurationSec" = p_duration_sec,
        "Width" = p_width, "Height" = p_height
    WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_mux_upload_id(p_clip_id text, p_upload_id text)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET "MuxUploadId" = p_upload_id WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_archive(p_clip_id text)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "IsArchived" = true, "ArchivedAt" = NOW(),
        "PlaybackIdSigned" = NULL, "MuxAssetId" = NULL
    WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_last_sold(p_clip_id text)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET "LastSoldAt" = NOW() WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_d_clip(p_clip_id text, p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM "Clips" WHERE "Id" = p_clip_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_archive_candidates(p_days integer)
RETURNS TABLE ("Id" text, "MuxAssetId" text) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."MuxAssetId" FROM "Clips" c
    WHERE c."IsArchived" = false
      AND c."MuxAssetId" IS NOT NULL
      AND c."MuxAssetId" NOT LIKE 'errored:%'
      AND (
        (c."LastSoldAt" IS NULL     AND c."PublishedAt" < NOW() - (p_days || ' days')::interval)
        OR
        (c."LastSoldAt" IS NOT NULL AND c."LastSoldAt"  < NOW() - (p_days || ' days')::interval)
      );
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- COLLECTIONS
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_collections_by_seller(p_seller_id integer)
RETURNS TABLE (
    "Id" text, "Name" text, "Date" date, "Location" text, "Summary" text,
    "DefaultPriceCents" integer, "DefaultPriceCommercialCents" integer,
    "DefaultAllowGifSale" boolean, "DefaultGifPriceCents" integer,
    "HeroClipId" text, "CreatedAt" timestamptz, "ClipCount" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Name", c."Date", c."Location", c."Summary",
           c."DefaultPriceCents", c."DefaultPriceCommercialCents",
           c."DefaultAllowGifSale", c."DefaultGifPriceCents",
           c."HeroClipId", c."CreatedAt", COUNT(cl."Id")::integer
    FROM "Collections" c
    LEFT JOIN "Clips" cl ON cl."CollectionId" = c."Id" AND cl."IsArchived" = false
    WHERE c."SellerId" = p_seller_id
    GROUP BY c."Id" ORDER BY c."Date" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_collection(
    p_id text, p_seller_id integer, p_name text, p_date date,
    p_location text, p_summary text, p_default_price_cents integer,
    p_default_price_commercial_cents integer, p_default_allow_gif_sale boolean,
    p_default_gif_price_cents integer
) RETURNS void AS $$
BEGIN
    INSERT INTO "Collections"
        ("Id", "SellerId", "Name", "Date", "Location", "Summary",
         "DefaultPriceCents", "DefaultPriceCommercialCents",
         "DefaultAllowGifSale", "DefaultGifPriceCents", "CreatedAt")
    VALUES
        (p_id, p_seller_id, p_name, p_date, p_location, p_summary,
         p_default_price_cents, p_default_price_commercial_cents,
         p_default_allow_gif_sale, p_default_gif_price_cents, NOW());
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_collection(
    p_collection_id text, p_seller_id integer, p_name text, p_date date,
    p_location text, p_summary text, p_default_price_cents integer,
    p_default_price_commercial_cents integer, p_default_allow_gif_sale boolean,
    p_default_gif_price_cents integer, p_hero_clip_id text
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Collections" SET
        "Name" = p_name, "Date" = p_date, "Location" = p_location,
        "Summary" = p_summary, "DefaultPriceCents" = p_default_price_cents,
        "DefaultPriceCommercialCents" = p_default_price_commercial_cents,
        "DefaultAllowGifSale" = p_default_allow_gif_sale,
        "DefaultGifPriceCents" = p_default_gif_price_cents,
        "HeroClipId" = p_hero_clip_id
    WHERE "Id" = p_collection_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_collection_clip_assets(p_collection_id text, p_seller_id integer)
RETURNS TABLE ("Id" text, "MuxAssetId" text, "ThumbnailFileName" text, "MasterFileName" text) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."MuxAssetId", c."ThumbnailFileName", c."MasterFileName"
    FROM "Clips" c
    WHERE c."CollectionId" = p_collection_id AND c."SellerId" = p_seller_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_d_collection(p_collection_id text, p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM "Collections" WHERE "Id" = p_collection_id AND "SellerId" = p_seller_id;
END;
$$;

-- ─────────────────────────────────────────────────────────────
-- PURCHASES
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_purchase_snapshot(p_clip_id text)
RETURNS TABLE (
    "ClipTitle" text, "CollectionName" text, "CollectionDate" date,
    "RecordingStartedAt" timestamptz, "DurationSec" double precision,
    "MasterFileName" text, "ThumbnailFileName" text
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Title", col."Name", col."Date",
           c."RecordingStartedAt", c."DurationSec",
           c."MasterFileName", c."ThumbnailFileName"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."Id" = p_clip_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_purchase(
    p_user_id text, p_clip_id text, p_seller_id integer,
    p_clip_title text, p_collection_name text, p_collection_date date,
    p_recording_started_at timestamptz, p_duration_sec double precision,
    p_master_file_name text, p_thumbnail_file_name text,
    p_stripe_session_id text, p_order_id text,
    p_price_paid_cents integer, p_platform_fee_cents integer, p_seller_payout_cents integer,
    p_license_type integer, p_customer_email text, p_customer_name text,
    p_is_gif boolean, p_gif_start_time double precision, p_gif_end_time double precision
) RETURNS integer AS $$
DECLARE v_id integer;
BEGIN
    INSERT INTO "Purchases"
        ("UserId", "ClipId", "SellerId", "ClipTitle", "CollectionName", "CollectionDate",
         "ClipRecordingStartedAt", "ClipDurationSec", "ClipMasterFileName", "ClipThumbnailFileName",
         "StripeSessionId", "OrderId", "PricePaidCents", "PlatformFeeCents", "SellerPayoutCents",
         "LicenseType", "CustomerEmail", "CustomerName", "IsGif", "GifStartTime", "GifEndTime",
         "FulfillmentStatus", "CreatedAt")
    VALUES
        (p_user_id, p_clip_id, p_seller_id, p_clip_title, p_collection_name, p_collection_date,
         p_recording_started_at, p_duration_sec, p_master_file_name, p_thumbnail_file_name,
         p_stripe_session_id, p_order_id, p_price_paid_cents, p_platform_fee_cents, p_seller_payout_cents,
         p_license_type, p_customer_email, p_customer_name, p_is_gif, p_gif_start_time, p_gif_end_time,
         0, NOW())
    RETURNING "Id" INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_purchase_fulfill(
    p_purchase_id integer, p_high_res_download_url text, p_mux_asset_id text
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Purchases" SET
        "FulfillmentStatus" = 1, "HighResDownloadUrl" = p_high_res_download_url,
        "FulfillmentMuxAssetId" = p_mux_asset_id, "FulfilledAt" = NOW()
    WHERE "Id" = p_purchase_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_seller_sales_summary()
RETURNS TABLE (
    "SellerId" integer, "DisplayName" text, "Slug" text,
    "SalesCount" bigint, "TotalRevenueCents" bigint,
    "PlatformFeeCents" bigint, "SellerPayoutCents" bigint
) AS $$
BEGIN
    RETURN QUERY
    SELECT p."SellerId", sf."DisplayName", sf."Slug",
           COUNT(p."Id"), SUM(p."PricePaidCents"),
           SUM(p."PlatformFeeCents"), SUM(p."SellerPayoutCents")
    FROM "Purchases" p
    JOIN "Storefronts" sf ON sf."SellerId" = p."SellerId"
    WHERE p."SellerId" IS NOT NULL
    GROUP BY p."SellerId", sf."DisplayName", sf."Slug"
    ORDER BY SUM(p."PricePaidCents") DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_daily_revenue(p_days integer)
RETURNS TABLE ("Date" date, "TotalCents" bigint) AS $$
BEGIN
    RETURN QUERY
    SELECT DATE("CreatedAt"), SUM("PricePaidCents")
    FROM "Purchases"
    WHERE "CreatedAt" >= NOW() - (p_days || ' days')::interval
    GROUP BY DATE("CreatedAt") ORDER BY DATE("CreatedAt");
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- MARKETPLACE
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_storefront(p_slug text)
RETURNS TABLE (
    "Slug" text, "DisplayName" text, "LogoUrl" text, "BannerUrl" text,
    "AccentColor" text, "Bio" text, "IsTrusted" boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT sf."Slug", sf."DisplayName", sf."LogoUrl", sf."BannerUrl",
           sf."AccentColor", sf."Bio", s."IsTrusted"
    FROM "Storefronts" sf
    JOIN "Sellers" s ON s."Id" = sf."SellerId"
    WHERE sf."Slug" = p_slug AND sf."IsPublished" = true AND s."IsTrusted" = true;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_storefront_clips(p_slug text)
RETURNS TABLE (
    "Id" text, "Title" text, "PlaybackIdTeaser" text, "ThumbnailFileName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer, "DurationSec" double precision,
    "CollectionName" text, "StorefrontSlug" text
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", col."Name", sf."Slug"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id"  = c."CollectionId"
    JOIN "Storefronts"  sf  ON sf."SellerId" = c."SellerId"
    WHERE c."SellerId" = (SELECT sf2."SellerId" FROM "Storefronts" sf2 WHERE sf2."Slug" = p_slug)
      AND c."IsArchived" = false AND c."PlaybackIdTeaser" IS NOT NULL
    ORDER BY c."PublishedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_marketplace_clips(p_search_term text, p_page_size integer, p_offset integer)
RETURNS TABLE (
    "Id" text, "Title" text, "PlaybackIdTeaser" text, "ThumbnailFileName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer, "DurationSec" double precision,
    "CollectionName" text, "StorefrontSlug" text
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", col."Name", sf."Slug"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id"  = c."CollectionId"
    JOIN "Storefronts"  sf  ON sf."SellerId" = c."SellerId"
    JOIN "Sellers"      s   ON s."Id"   = c."SellerId"
    WHERE c."IsArchived" = false AND c."PlaybackIdTeaser" IS NOT NULL
      AND s."IsTrusted" = true
      AND (p_search_term IS NULL OR c."Title" ILIKE '%' || p_search_term || '%')
    ORDER BY c."PublishedAt" DESC
    LIMIT p_page_size OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_marketplace_clips_count(p_search_term text)
RETURNS integer AS $$
DECLARE v_count integer;
BEGIN
    SELECT COUNT(c."Id")::integer INTO v_count
    FROM "Clips" c
    JOIN "Sellers" s ON s."Id" = c."SellerId"
    WHERE c."IsArchived" = false AND c."PlaybackIdTeaser" IS NOT NULL
      AND s."IsTrusted" = true
      AND (p_search_term IS NULL OR c."Title" ILIKE '%' || p_search_term || '%');
    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- ADMIN
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_admin_sellers()
RETURNS TABLE (
    "Id" integer, "Email" text, "DisplayName" text, "Slug" text,
    "IsTrusted" boolean, "IsPublished" boolean, "CreatedAt" timestamptz,
    "ClipCount" integer, "SalesCount" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT s."Id", u."Email", sf."DisplayName", sf."Slug",
           s."IsTrusted", sf."IsPublished", s."CreatedAt",
           COUNT(DISTINCT c."Id")::integer, COUNT(DISTINCT p."Id")::integer
    FROM "Sellers" s
    JOIN "AspNetUsers"  u   ON u."Id"        = s."UserId"
    JOIN "Storefronts"  sf  ON sf."SellerId" = s."Id"
    LEFT JOIN "Clips"   c   ON c."SellerId"  = s."Id"
    LEFT JOIN "Purchases" p ON p."SellerId"  = s."Id"
    GROUP BY s."Id", u."Email", sf."DisplayName", sf."Slug",
             s."IsTrusted", sf."IsPublished", s."CreatedAt"
    ORDER BY s."CreatedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_seller_approve(p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN UPDATE "Sellers" SET "IsTrusted" = true  WHERE "Id" = p_seller_id; END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_seller_revoke(p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN UPDATE "Sellers" SET "IsTrusted" = false WHERE "Id" = p_seller_id; END;
$$;

CREATE OR REPLACE FUNCTION cc_s_platform_stats()
RETURNS TABLE (
    "TotalSellers" integer, "TrustedSellers" integer, "TotalClips" integer,
    "TotalPurchases" integer, "TotalRevenueCents" bigint,
    "TotalPlatformFees" bigint, "TotalPayouts" bigint
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        (SELECT COUNT(*)::integer FROM "Sellers"),
        (SELECT COUNT(*)::integer FROM "Sellers"   WHERE "IsTrusted" = true),
        (SELECT COUNT(*)::integer FROM "Clips"     WHERE "IsArchived" = false),
        (SELECT COUNT(*)::integer FROM "Purchases"),
        (SELECT COALESCE(SUM("PricePaidCents"),    0) FROM "Purchases"),
        (SELECT COALESCE(SUM("PlatformFeeCents"),  0) FROM "Purchases"),
        (SELECT COALESCE(SUM("SellerPayoutCents"), 0) FROM "Purchases");
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- SETTINGS + USAGE
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE cc_u_setting(p_key text, p_value text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "Settings" ("Key", "Value", "UpdatedAt")
    VALUES (p_key, p_value, NOW())
    ON CONFLICT ("Key") DO UPDATE
    SET "Value" = EXCLUDED."Value", "UpdatedAt" = NOW();
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_usage_increment(p_ip_address text, p_date date, p_user_id text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "DailyWatchUsages" ("IpAddress", "Date", "UserId", "TokenRequestCount")
    VALUES (p_ip_address, p_date, p_user_id, 1)
    ON CONFLICT ("IpAddress", "Date") DO UPDATE
    SET "TokenRequestCount" = "DailyWatchUsages"."TokenRequestCount" + 1,
        "UserId" = COALESCE("DailyWatchUsages"."UserId", EXCLUDED."UserId");
END;
$$;
```

---

## MODELS

### Models/Auth/AuthModels.cs
```csharp
namespace ClipCore.API.Models.Auth;
public class AuthenticateRequest  { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
public class AuthenticateResponse { public string Token { get; set; } = ""; public string Email { get; set; } = ""; public string Role { get; set; } = ""; public int? SellerId { get; set; } }
public class RegisterSellerRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string DisplayName { get; set; } = ""; public string Slug { get; set; } = ""; }
public class ForgotPasswordRequest { public string Email { get; set; } = ""; }
public class ResetPasswordRequest  { public string Email { get; set; } = ""; public string Token { get; set; } = ""; public string Password { get; set; } = ""; }
```

### Models/Clip/ClipModels.cs
```csharp
namespace ClipCore.API.Models.Clip;

public class ClipItem {
    public string  Id { get; set; } = "";
    public string  Title { get; set; } = "";
    public string  CollectionId { get; set; } = "";
    public string? CollectionName { get; set; }
    public int     PriceCents { get; set; }
    public int     PriceCommercialCents { get; set; }
    public bool    AllowGifSale { get; set; }
    public int     GifPriceCents { get; set; }
    public double? DurationSec { get; set; }
    public string? PlaybackIdTeaser { get; set; }
    public string? ThumbnailFileName { get; set; }
    public bool    IsArchived { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class ClipDetail : ClipItem {
    public string  PlaybackIdSigned { get; set; } = "";
    public string? MuxAssetId { get; set; }
    public string? MuxUploadId { get; set; }
    public string? MasterFileName { get; set; }
    public string  TagsJson { get; set; } = "[]";
    public DateTime? RecordingStartedAt { get; set; }
    public int?    Width { get; set; }
    public int?    Height { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? LastSoldAt { get; set; }
}

public class CreateClipRequest {
    public string CollectionId { get; set; } = "";
    public string Title { get; set; } = "";
    public int    PriceCents { get; set; }
    public int    PriceCommercialCents { get; set; }
    public bool   AllowGifSale { get; set; }
    public int    GifPriceCents { get; set; } = 199;
    public string TagsJson { get; set; } = "[]";
}

public class UpdateClipRequest {
    public string ClipId { get; set; } = "";
    public string Title { get; set; } = "";
    public int    PriceCents { get; set; }
    public int    PriceCommercialCents { get; set; }
    public bool   AllowGifSale { get; set; }
    public int    GifPriceCents { get; set; }
    public string TagsJson { get; set; } = "[]";
}

public class BatchSettingsRequest {
    public string CollectionId { get; set; } = "";
    public int    PriceCents { get; set; }
    public int    PriceCommercialCents { get; set; }
    public bool   AllowGifSale { get; set; }
    public int    GifPriceCents { get; set; }
}
```

### Models/Collection/CollectionModels.cs
```csharp
namespace ClipCore.API.Models.Collection;

public class CollectionItem {
    public string   Id { get; set; } = "";
    public string   Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string?  Location { get; set; }
    public string?  Summary { get; set; }
    public int      DefaultPriceCents { get; set; }
    public int      DefaultPriceCommercialCents { get; set; }
    public bool     DefaultAllowGifSale { get; set; }
    public int      DefaultGifPriceCents { get; set; }
    public string?  HeroClipId { get; set; }
    public int      ClipCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CollectionDetail : CollectionItem {
    public List<ClipCore.API.Models.Clip.ClipItem> Clips { get; set; } = new();
}

public class CreateCollectionRequest {
    public string   Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string?  Location { get; set; }
    public string?  Summary { get; set; }
    public int      DefaultPriceCents { get; set; } = 1000;
    public int      DefaultPriceCommercialCents { get; set; } = 4900;
    public bool     DefaultAllowGifSale { get; set; }
    public int      DefaultGifPriceCents { get; set; } = 199;
}

public class UpdateCollectionRequest : CreateCollectionRequest {
    public string  CollectionId { get; set; } = "";
    public string? HeroClipId   { get; set; }
}
```

### Models/Seller/SellerModels.cs
```csharp
namespace ClipCore.API.Models.Seller;

public class SellerProfile {
    public int    Id { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public bool   IsTrusted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string  Slug { get; set; } = "";
    public string  DisplayName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool    IsPublished { get; set; }
}

public class StorefrontSettingsRequest {
    public string  DisplayName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool    IsPublished { get; set; }
}

public class SellerSalesStats {
    public int TotalSales { get; set; }
    public int TotalRevenueCents { get; set; }
    public int TotalPayoutCents { get; set; }
    public int PendingFulfillment { get; set; }
}
```

### Models/Purchase/PurchaseModels.cs
```csharp
using ClipCore.Core.Entities;
namespace ClipCore.API.Models.Purchase;

public class PurchaseItem {
    public int     Id { get; set; }
    public string? ClipId { get; set; }
    public string  ClipTitle { get; set; } = "";
    public string? CollectionName { get; set; }
    public DateOnly? CollectionDate { get; set; }
    public int     PricePaidCents { get; set; }
    public LicenseType LicenseType { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? HighResDownloadUrl { get; set; }
    public bool    IsGif { get; set; }
}

public class PurchaseDetail : PurchaseItem {
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public int     PlatformFeeCents { get; set; }
    public int     SellerPayoutCents { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? StripeSessionId { get; set; }
    public string? OrderId { get; set; }
    public double? GifStartTime { get; set; }
    public double? GifEndTime { get; set; }
    public string? BrandedPlaybackId { get; set; }
}

public class CreateCheckoutRequest {
    public string ClipId { get; set; } = "";
    public LicenseType LicenseType { get; set; } = LicenseType.Personal;
    public string? PromoCode { get; set; }
    public double? GifStartTime { get; set; }
    public double? GifEndTime { get; set; }
}

public class SellerSalesSummary {
    public int    SellerId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Slug { get; set; } = "";
    public long   SalesCount { get; set; }
    public long   TotalRevenueCents { get; set; }
    public long   PlatformFeeCents { get; set; }
    public long   SellerPayoutCents { get; set; }
}

public class DailyRevenue {
    public DateOnly Date { get; set; }
    public long     TotalCents { get; set; }
}
```

### Models/Marketplace/MarketplaceModels.cs
```csharp
namespace ClipCore.API.Models.Marketplace;

public class StorefrontPublic {
    public string  Slug { get; set; } = "";
    public string  DisplayName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? AccentColor { get; set; }
    public string? Bio { get; set; }
    public bool    IsTrusted { get; set; }
    public List<MarketplaceClip> Clips { get; set; } = new();
}

public class MarketplaceClip {
    public string  Id { get; set; } = "";
    public string  Title { get; set; } = "";
    public string? PlaybackIdTeaser { get; set; }
    public string? ThumbnailFileName { get; set; }
    public int     PriceCents { get; set; }
    public int     PriceCommercialCents { get; set; }
    public bool    AllowGifSale { get; set; }
    public int     GifPriceCents { get; set; }
    public double? DurationSec { get; set; }
    public string? CollectionName { get; set; }
    public string  StorefrontSlug { get; set; } = "";
}

public class MarketplaceSearchRequest {
    public string? SearchTerm { get; set; }
    public int     Page { get; set; } = 1;
    public int     PageSize { get; set; } = 24;
}

public class MarketplaceSearchResponse {
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<MarketplaceClip> Clips { get; set; } = new();
}
```

### Models/Admin/AdminModels.cs
```csharp
namespace ClipCore.API.Models.Admin;

public class AdminSellerItem {
    public int    Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool   IsTrusted { get; set; }
    public bool   IsPublished { get; set; }
    public int    ClipCount { get; set; }
    public int    SalesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlatformStats {
    public int  TotalSellers { get; set; }
    public int  TrustedSellers { get; set; }
    public int  TotalClips { get; set; }
    public int  TotalPurchases { get; set; }
    public long TotalRevenueCents { get; set; }
    public long TotalPlatformFees { get; set; }
    public long TotalPayouts { get; set; }
}

public class ApproveSellerRequest { public int SellerId { get; set; } }
```

---

## INTERFACES

```csharp
// ISellerData.cs
public interface ISellerData {
    Task<SellerProfile?> GetSellerProfile(int sellerId);
    Task<SellerProfile?> GetSellerProfileByUserId(string userId);
    Task<int> CreateSeller(string userId);
    Task CreateStorefront(int sellerId, string slug, string displayName);
    Task UpdateStorefrontSettings(int sellerId, StorefrontSettingsRequest request);
    Task<SellerSalesStats> GetSellerSalesStats(int sellerId);
    Task<bool> SlugExists(string slug);
}

// IClipData.cs
public interface IClipData {
    Task<IEnumerable<ClipItem>> GetClipsBySeller(int sellerId);
    Task<IEnumerable<ClipItem>> GetClipsByCollection(string collectionId);
    Task<ClipDetail?> GetClipDetail(string clipId);
    Task<ClipDetail?> GetClipDetailForSeller(string clipId, int sellerId);
    Task<IEnumerable<ClipItem>> SearchAsync(string query);
    Task<string> CreateClip(int sellerId, CreateClipRequest request);
    Task UpdateClip(int sellerId, UpdateClipRequest request);
    Task UpdateBatchSettings(string collectionId, int sellerId, int priceCents, int priceCommercialCents, bool allowGif, int gifPriceCents);
    Task DeleteClip(string clipId, int sellerId);
    Task SetMuxData(string clipId, string muxAssetId, string playbackIdSigned, string? playbackIdTeaser, double? durationSec, int? width, int? height);
    Task SetMuxUploadId(string clipId, string muxUploadId);
    Task ArchiveClip(string clipId);
    Task UpdateLastSoldAt(string clipId);
    Task<IEnumerable<ArchiveCandidateClip>> GetArchiveCandidates(int daysSinceLastSale);
}

// ICollectionData.cs
public interface ICollectionData {
    Task<IEnumerable<CollectionItem>> GetCollectionsBySeller(int sellerId);
    Task<CollectionDetail?> GetCollectionDetail(string collectionId, int sellerId);
    Task<string> CreateCollection(int sellerId, CreateCollectionRequest request);
    Task UpdateCollection(int sellerId, UpdateCollectionRequest request);
    Task DeleteCollection(string collectionId, int sellerId);
}

// IPurchaseData.cs
public interface IPurchaseData {
    Task<IEnumerable<PurchaseItem>> GetPurchasesByUser(string userId);
    Task<IEnumerable<PurchaseItem>> GetPurchasesByEmail(string email);
    Task<IEnumerable<PurchaseItem>> GetPurchasesBySeller(int sellerId);
    Task<IEnumerable<PurchaseDetail>> GetBySessionId(string sessionId);
    Task<PurchaseDetail?> GetPurchaseDetail(int purchaseId);
    Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license);
    Task<bool> HasPurchasedGifAsync(string? userId, string clipId);
    Task<IEnumerable<PurchaseDetail>> ListFiltered(FulfillmentStatus? status, DateTime? since, string? search);
    Task<int> CreatePurchase(string? userId, string clipId, int sellerId, int pricePaidCents, int platformFeeCents, int sellerPayoutCents, string stripeSessionId, string orderId, LicenseType licenseType, string? customerEmail, string? customerName, bool isGif, double? gifStartTime, double? gifEndTime);
    Task FulfillPurchase(int purchaseId, string highResDownloadUrl, string? muxAssetId);
    Task<IEnumerable<SellerSalesSummary>> GetSellerSalesSummary();
    Task<IEnumerable<DailyRevenue>> GetDailyRevenue(int days);
    Task<IEnumerable<PurchaseItem>> GetRecentSales(int count);
    Task<bool> HasUserPurchasedClip(string? userId, string customerEmail, string clipId);
}

// IMarketplaceData.cs
public interface IMarketplaceData {
    Task<StorefrontPublic?> GetStorefront(string slug);
    Task<MarketplaceSearchResponse> SearchClips(MarketplaceSearchRequest request);
    Task<IEnumerable<MarketplaceClip>> GetFeaturedClips(int limit = 24);
}

// IAdminData.cs
public interface IAdminData {
    Task<IEnumerable<AdminSellerItem>> GetAllSellers();
    Task ApproveSeller(int sellerId);
    Task RevokeSeller(int sellerId);
    Task<PlatformStats> GetPlatformStats();
}

// IPromoCodeData.cs
public interface IPromoCodeData {
    Task<PromoCode?> GetByCodeAsync(string code);
    Task<IEnumerable<PromoCode>> ListAsync();
    Task AddAsync(PromoCode promo);
    Task IncrementUsageAsync(int id);
    Task DeleteAsync(int id);
}

// ISettingsData.cs
public interface ISettingsData {
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value);
    Task<IEnumerable<Setting>> ListAllAsync();
}

// IUsageData.cs
public interface IUsageData {
    Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date);
    Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null);
}
```

---

## DATA CLASSES

### Data/SellerData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Seller;
namespace ClipCore.API.Data;

public class SellerData : ISellerData {
    private readonly ISqlDataAccess _db;
    public SellerData(ISqlDataAccess db) => _db = db;

    public Task<SellerProfile?> GetSellerProfile(int sellerId) =>
        _db.LoadSingle<SellerProfile, dynamic>("SELECT * FROM cc_s_seller_profile(@SellerId)", new { SellerId = sellerId });

    public Task<SellerProfile?> GetSellerProfileByUserId(string userId) =>
        _db.LoadSingle<SellerProfile, dynamic>("SELECT * FROM cc_s_seller_profile_by_user(@UserId)", new { UserId = userId });

    public async Task<int> CreateSeller(string userId) =>
        await _db.ExecuteScalar<int, dynamic>("SELECT cc_i_seller(@UserId)", new { UserId = userId })
            ?? throw new InvalidOperationException("Failed to create seller");

    public Task CreateStorefront(int sellerId, string slug, string displayName) =>
        _db.SaveData("CALL cc_i_storefront(@SellerId, @Slug, @DisplayName)", new { SellerId = sellerId, Slug = slug, DisplayName = displayName });

    public Task UpdateStorefrontSettings(int sellerId, StorefrontSettingsRequest r) =>
        _db.SaveData("CALL cc_u_storefront_settings(@SellerId, @DisplayName, @LogoUrl, @BannerUrl, @AccentColor, @Bio, @IsPublished)",
            new { SellerId = sellerId, r.DisplayName, r.LogoUrl, r.BannerUrl, r.AccentColor, r.Bio, r.IsPublished });

    public async Task<SellerSalesStats> GetSellerSalesStats(int sellerId) =>
        await _db.LoadSingle<SellerSalesStats, dynamic>("SELECT * FROM cc_s_seller_sales_stats(@SellerId)", new { SellerId = sellerId })
            ?? new SellerSalesStats();

    public async Task<bool> SlugExists(string slug) =>
        await _db.ExecuteScalar<bool, dynamic>("SELECT cc_s_slug_exists(@Slug)", new { Slug = slug }) ?? false;
}
```

### Data/ClipData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;
namespace ClipCore.API.Data;

public class ClipData : IClipData {
    private readonly ISqlDataAccess _db;
    public ClipData(ISqlDataAccess db) => _db = db;

    public Task<IEnumerable<ClipItem>> GetClipsBySeller(int sellerId) =>
        _db.LoadData<ClipItem, dynamic>("SELECT * FROM cc_s_clips_by_seller(@SellerId)", new { SellerId = sellerId });

    public Task<IEnumerable<ClipItem>> GetClipsByCollection(string collectionId) =>
        _db.LoadData<ClipItem, dynamic>("SELECT * FROM cc_s_clips_by_collection(@CollectionId)", new { CollectionId = collectionId });

    public Task<ClipDetail?> GetClipDetail(string clipId) =>
        _db.LoadSingle<ClipDetail, dynamic>("SELECT * FROM cc_s_clip_detail(@ClipId)", new { ClipId = clipId });

    public Task<ClipDetail?> GetClipDetailForSeller(string clipId, int sellerId) =>
        _db.LoadSingle<ClipDetail, dynamic>(
            @"SELECT c.*, col.""Name"" AS ""CollectionName"" FROM ""Clips"" c
              JOIN ""Collections"" col ON col.""Id"" = c.""CollectionId""
              WHERE c.""Id"" = @ClipId AND c.""SellerId"" = @SellerId",
            new { ClipId = clipId, SellerId = sellerId });

    public Task<IEnumerable<ClipItem>> SearchAsync(string query) =>
        _db.LoadData<ClipItem, dynamic>("SELECT * FROM cc_s_clips_search(@Query)", new { Query = query });

    public async Task<string> CreateClip(int sellerId, CreateClipRequest r) {
        var id = Guid.NewGuid().ToString();
        await _db.SaveData("SELECT cc_i_clip(@Id, @CollectionId, @SellerId, @Title, @PriceCents, @PriceCommercialCents, @AllowGifSale, @GifPriceCents, @TagsJson)",
            new { Id = id, r.CollectionId, SellerId = sellerId, r.Title, r.PriceCents, r.PriceCommercialCents, r.AllowGifSale, r.GifPriceCents, r.TagsJson });
        return id;
    }

    public Task UpdateClip(int sellerId, UpdateClipRequest r) =>
        _db.SaveData("CALL cc_u_clip(@ClipId, @SellerId, @Title, @PriceCents, @PriceCommercialCents, @AllowGifSale, @GifPriceCents, @TagsJson)",
            new { ClipId = r.ClipId, SellerId = sellerId, r.Title, r.PriceCents, r.PriceCommercialCents, r.AllowGifSale, r.GifPriceCents, r.TagsJson });

    public Task UpdateBatchSettings(string collectionId, int sellerId, int priceCents, int priceCommercialCents, bool allowGif, int gifPriceCents) =>
        _db.SaveData("CALL cc_u_clip_batch_settings(@CollectionId, @SellerId, @PriceCents, @PriceCommercialCents, @AllowGif, @GifPriceCents)",
            new { CollectionId = collectionId, SellerId = sellerId, PriceCents = priceCents, PriceCommercialCents = priceCommercialCents, AllowGif = allowGif, GifPriceCents = gifPriceCents });

    public Task DeleteClip(string clipId, int sellerId) =>
        _db.SaveData("CALL cc_d_clip(@ClipId, @SellerId)", new { ClipId = clipId, SellerId = sellerId });

    public Task SetMuxData(string clipId, string muxAssetId, string playbackIdSigned, string? playbackIdTeaser, double? durationSec, int? width, int? height) =>
        _db.SaveData("CALL cc_u_clip_mux_data(@ClipId, @MuxAssetId, @PlaybackIdSigned, @PlaybackIdTeaser, @DurationSec, @Width, @Height)",
            new { ClipId = clipId, MuxAssetId = muxAssetId, PlaybackIdSigned = playbackIdSigned, PlaybackIdTeaser = playbackIdTeaser, DurationSec = durationSec, Width = width, Height = height });

    public Task SetMuxUploadId(string clipId, string muxUploadId) =>
        _db.SaveData("CALL cc_u_clip_mux_upload_id(@ClipId, @MuxUploadId)", new { ClipId = clipId, MuxUploadId = muxUploadId });

    public Task ArchiveClip(string clipId) =>
        _db.SaveData("CALL cc_u_clip_archive(@ClipId)", new { ClipId = clipId });

    public Task UpdateLastSoldAt(string clipId) =>
        _db.SaveData("CALL cc_u_clip_last_sold(@ClipId)", new { ClipId = clipId });

    public Task<IEnumerable<ArchiveCandidateClip>> GetArchiveCandidates(int days) =>
        _db.LoadData<ArchiveCandidateClip, dynamic>("SELECT * FROM cc_s_archive_candidates(@Days)", new { Days = days });
}

public class ArchiveCandidateClip { public string Id { get; set; } = ""; public string? MuxAssetId { get; set; } }
```

### Data/CollectionData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;
using ClipCore.API.Models.Collection;
using ClipCore.API.Services;
namespace ClipCore.API.Data;

public class CollectionData : ICollectionData {
    private readonly ISqlDataAccess _db;
    private readonly IMuxService _mux;
    private readonly IR2StorageService _r2;

    public CollectionData(ISqlDataAccess db, IMuxService mux, IR2StorageService r2) {
        _db = db; _mux = mux; _r2 = r2;
    }

    public Task<IEnumerable<CollectionItem>> GetCollectionsBySeller(int sellerId) =>
        _db.LoadData<CollectionItem, dynamic>("SELECT * FROM cc_s_collections_by_seller(@SellerId)", new { SellerId = sellerId });

    public async Task<CollectionDetail?> GetCollectionDetail(string collectionId, int sellerId) {
        var coll = await _db.LoadSingle<CollectionDetail, dynamic>(
            @"SELECT c.""Id"", c.""Name"", c.""Date"", c.""Location"", c.""Summary"",
                     c.""DefaultPriceCents"", c.""DefaultPriceCommercialCents"",
                     c.""DefaultAllowGifSale"", c.""DefaultGifPriceCents"",
                     c.""HeroClipId"", c.""CreatedAt"",
                     COUNT(cl.""Id"")::int AS ""ClipCount""
              FROM ""Collections"" c
              LEFT JOIN ""Clips"" cl ON cl.""CollectionId"" = c.""Id"" AND cl.""IsArchived"" = false
              WHERE c.""Id"" = @CollectionId AND c.""SellerId"" = @SellerId GROUP BY c.""Id""",
            new { CollectionId = collectionId, SellerId = sellerId });
        if (coll is null) return null;
        coll.Clips = (await _db.LoadData<ClipItem, dynamic>(
            "SELECT * FROM cc_s_clips_by_collection(@CollectionId)",
            new { CollectionId = collectionId })).ToList();
        return coll;
    }

    public async Task<string> CreateCollection(int sellerId, CreateCollectionRequest r) {
        var id = Guid.NewGuid().ToString();
        await _db.SaveData("SELECT cc_i_collection(@Id, @SellerId, @Name, @Date, @Location, @Summary, @DefaultPriceCents, @DefaultPriceCommercialCents, @DefaultAllowGifSale, @DefaultGifPriceCents)",
            new { Id = id, SellerId = sellerId, r.Name, r.Date, r.Location, r.Summary, r.DefaultPriceCents, r.DefaultPriceCommercialCents, r.DefaultAllowGifSale, r.DefaultGifPriceCents });
        return id;
    }

    public Task UpdateCollection(int sellerId, UpdateCollectionRequest r) =>
        _db.SaveData("CALL cc_u_collection(@CollectionId, @SellerId, @Name, @Date, @Location, @Summary, @DefaultPriceCents, @DefaultPriceCommercialCents, @DefaultAllowGifSale, @DefaultGifPriceCents, @HeroClipId)",
            new { CollectionId = r.CollectionId, SellerId = sellerId, r.Name, r.Date, r.Location, r.Summary, r.DefaultPriceCents, r.DefaultPriceCommercialCents, r.DefaultAllowGifSale, r.DefaultGifPriceCents, r.HeroClipId });

    public async Task DeleteCollection(string collectionId, int sellerId) {
        // Load assets first, run Mux + R2 cleanup in parallel (matches CollectionRepository.DeleteAsync)
        var clips = await _db.LoadData<ClipAssets, dynamic>(
            "SELECT * FROM cc_s_collection_clip_assets(@CollectionId, @SellerId)",
            new { CollectionId = collectionId, SellerId = sellerId });

        await Task.WhenAll(clips.SelectMany(c => {
            var t = new List<Task>();
            if (!string.IsNullOrEmpty(c.MuxAssetId))        t.Add(Safe(() => _mux.DeleteAssetAsync(c.MuxAssetId)));
            if (!string.IsNullOrEmpty(c.ThumbnailFileName)) t.Add(Safe(() => _r2.DeleteAsync(c.ThumbnailFileName)));
            if (!string.IsNullOrEmpty(c.MasterFileName))    t.Add(Safe(() => _r2.DeleteAsync(c.MasterFileName)));
            return t;
        }));

        await _db.SaveData("CALL cc_d_collection(@CollectionId, @SellerId)", new { CollectionId = collectionId, SellerId = sellerId });
    }

    private static async Task Safe(Func<Task> a) { try { await a(); } catch { } }

    private class ClipAssets { public string Id { get; set; } = ""; public string? MuxAssetId { get; set; } public string? ThumbnailFileName { get; set; } public string? MasterFileName { get; set; } }
}
```

### Data/PurchaseData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Purchase;
using ClipCore.Core.Entities;
namespace ClipCore.API.Data;

public class PurchaseData : IPurchaseData {
    private readonly ISqlDataAccess _db;
    public PurchaseData(ISqlDataAccess db) => _db = db;

    public Task<IEnumerable<PurchaseItem>> GetPurchasesByUser(string userId) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" WHERE ""UserId""=@UserId ORDER BY ""CreatedAt"" DESC",
            new { UserId = userId });

    public Task<IEnumerable<PurchaseItem>> GetPurchasesByEmail(string email) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" WHERE LOWER(""CustomerEmail"")=LOWER(@Email) ORDER BY ""CreatedAt"" DESC",
            new { Email = email });

    public Task<IEnumerable<PurchaseItem>> GetPurchasesBySeller(int sellerId) =>
        _db.LoadData<PurchaseItem, dynamic>(
            @"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" WHERE ""SellerId""=@SellerId ORDER BY ""CreatedAt"" DESC",
            new { SellerId = sellerId });

    public Task<IEnumerable<PurchaseDetail>> GetBySessionId(string sessionId) =>
        _db.LoadData<PurchaseDetail, dynamic>(@"SELECT * FROM ""Purchases"" WHERE ""StripeSessionId""=@SessionId", new { SessionId = sessionId });

    public Task<PurchaseDetail?> GetPurchaseDetail(int purchaseId) =>
        _db.LoadSingle<PurchaseDetail, dynamic>(@"SELECT * FROM ""Purchases"" WHERE ""Id""=@PurchaseId", new { PurchaseId = purchaseId });

    public async Task<bool> HasPurchasedAsync(string? userId, string clipId, LicenseType license) {
        if (string.IsNullOrEmpty(userId)) return false;
        var n = await _db.ExecuteScalar<int, dynamic>(@"SELECT COUNT(1) FROM ""Purchases"" WHERE ""UserId""=@UserId AND ""ClipId""=@ClipId AND ""LicenseType""=@LicenseType", new { UserId = userId, ClipId = clipId, LicenseType = (int)license });
        return n > 0;
    }

    public async Task<bool> HasPurchasedGifAsync(string? userId, string clipId) {
        if (string.IsNullOrEmpty(userId)) return false;
        var n = await _db.ExecuteScalar<int, dynamic>(@"SELECT COUNT(1) FROM ""Purchases"" WHERE ""UserId""=@UserId AND ""ClipId""=@ClipId AND ""IsGif""=true", new { UserId = userId, ClipId = clipId });
        return n > 0;
    }

    // Complex fuzzy search stays inline — too dynamic for a static function
    public async Task<IEnumerable<PurchaseDetail>> ListFiltered(FulfillmentStatus? status = null, DateTime? since = null, string? search = null) {
        var conds = new List<string>();
        var p     = new Dictionary<string, object?>();
        if (status.HasValue) { conds.Add(@"""FulfillmentStatus""=@Status"); p["Status"] = (int)status.Value; }
        if (since.HasValue && string.IsNullOrEmpty(search)) { conds.Add(@"""CreatedAt"">=@Since"); p["Since"] = since.Value; }
        if (!string.IsNullOrEmpty(search)) {
            var s = search.Trim().ToLower(); var sz = s.Replace("o","0"); var so = s.Replace("0","o");
            var sid = s.StartsWith("ord-") ? s[4..] : s; var sidz = sid.Replace("o","0"); var sido = sid.Replace("0","o");
            conds.Add(@"(LOWER(""CustomerEmail"") LIKE '%'||@S||'%' OR LOWER(""CustomerName"") LIKE '%'||@S||'%' OR LOWER(""ClipTitle"") LIKE '%'||@S||'%' OR LOWER(""CollectionName"") LIKE '%'||@S||'%' OR LOWER(""StripeSessionId"") LIKE ANY(ARRAY[@S,@SZ,@SO,@SId,@SIdZ,@SIdO]) OR LOWER(""OrderId"") LIKE ANY(ARRAY[@S,@SZ,@SO,@SId,@SIdZ,@SIdO]))");
            p["S"]=s; p["SZ"]=sz; p["SO"]=so; p["SId"]=sid; p["SIdZ"]=sidz; p["SIdO"]=sido;
        }
        var where = conds.Any() ? "WHERE " + string.Join(" AND ", conds) : "";
        return await _db.LoadData<PurchaseDetail, dynamic>($@"SELECT * FROM ""Purchases"" {where} ORDER BY ""CreatedAt"" DESC", p);
    }

    public async Task<int> CreatePurchase(string? userId, string clipId, int sellerId, int pricePaidCents, int platformFeeCents, int sellerPayoutCents, string stripeSessionId, string orderId, LicenseType licenseType, string? customerEmail, string? customerName, bool isGif, double? gifStartTime, double? gifEndTime) {
        var snap = await _db.LoadSingle<Snap, dynamic>("SELECT * FROM cc_s_purchase_snapshot(@ClipId)", new { ClipId = clipId });
        return await _db.ExecuteScalar<int, dynamic>(
            "SELECT cc_i_purchase(@UserId,@ClipId,@SellerId,@ClipTitle,@CollectionName,@CollectionDate,@RecordingStartedAt,@DurationSec,@MasterFileName,@ThumbnailFileName,@StripeSessionId,@OrderId,@PricePaidCents,@PlatformFeeCents,@SellerPayoutCents,@LicenseType,@CustomerEmail,@CustomerName,@IsGif,@GifStartTime,@GifEndTime)",
            new { UserId=userId, ClipId=clipId, SellerId=sellerId, ClipTitle=snap?.ClipTitle, CollectionName=snap?.CollectionName, CollectionDate=snap?.CollectionDate, RecordingStartedAt=snap?.RecordingStartedAt, DurationSec=snap?.DurationSec, MasterFileName=snap?.MasterFileName, ThumbnailFileName=snap?.ThumbnailFileName, StripeSessionId=stripeSessionId, OrderId=orderId, PricePaidCents=pricePaidCents, PlatformFeeCents=platformFeeCents, SellerPayoutCents=sellerPayoutCents, LicenseType=(int)licenseType, CustomerEmail=customerEmail, CustomerName=customerName, IsGif=isGif, GifStartTime=gifStartTime, GifEndTime=gifEndTime })
            ?? throw new InvalidOperationException("Failed to create purchase");
    }

    public Task FulfillPurchase(int purchaseId, string highResDownloadUrl, string? muxAssetId) =>
        _db.SaveData("CALL cc_u_purchase_fulfill(@PurchaseId,@HighResDownloadUrl,@MuxAssetId)", new { PurchaseId=purchaseId, HighResDownloadUrl=highResDownloadUrl, MuxAssetId=muxAssetId });

    public Task<IEnumerable<SellerSalesSummary>> GetSellerSalesSummary() =>
        _db.LoadData<SellerSalesSummary>("SELECT * FROM cc_s_seller_sales_summary()");

    public Task<IEnumerable<DailyRevenue>> GetDailyRevenue(int days) =>
        _db.LoadData<DailyRevenue, dynamic>("SELECT * FROM cc_s_daily_revenue(@Days)", new { Days = days });

    public Task<IEnumerable<PurchaseItem>> GetRecentSales(int count) =>
        _db.LoadData<PurchaseItem, dynamic>(@"SELECT ""Id"",""ClipId"",""ClipTitle"",""CollectionName"",""CollectionDate"",""PricePaidCents"",""LicenseType"",""FulfillmentStatus"",""CreatedAt"",""HighResDownloadUrl"",""IsGif"" FROM ""Purchases"" ORDER BY ""CreatedAt"" DESC LIMIT @Count", new { Count = count });

    public async Task<bool> HasUserPurchasedClip(string? userId, string customerEmail, string clipId) {
        var n = await _db.ExecuteScalar<int, dynamic>(@"SELECT COUNT(1) FROM ""Purchases"" WHERE ""ClipId""=@ClipId AND (""UserId""=@UserId OR LOWER(""CustomerEmail"")=LOWER(@CustomerEmail)) AND ""FulfillmentStatus""=1", new { ClipId=clipId, UserId=userId, CustomerEmail=customerEmail });
        return n > 0;
    }

    private class Snap { public string? ClipTitle { get; set; } public string? CollectionName { get; set; } public DateOnly? CollectionDate { get; set; } public DateTime? RecordingStartedAt { get; set; } public double? DurationSec { get; set; } public string? MasterFileName { get; set; } public string? ThumbnailFileName { get; set; } }
}
```

### Data/MarketplaceData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Marketplace;
namespace ClipCore.API.Data;

public class MarketplaceData : IMarketplaceData {
    private readonly ISqlDataAccess _db;
    public MarketplaceData(ISqlDataAccess db) => _db = db;

    public async Task<StorefrontPublic?> GetStorefront(string slug) {
        var sf = await _db.LoadSingle<StorefrontPublic, dynamic>("SELECT * FROM cc_s_storefront(@Slug)", new { Slug = slug });
        if (sf is null) return null;
        sf.Clips = (await _db.LoadData<MarketplaceClip, dynamic>("SELECT * FROM cc_s_storefront_clips(@Slug)", new { Slug = slug })).ToList();
        return sf;
    }

    public async Task<MarketplaceSearchResponse> SearchClips(MarketplaceSearchRequest req) {
        int offset = (req.Page - 1) * req.PageSize;
        var p = new { req.SearchTerm, req.PageSize, Offset = offset };
        var total = await _db.ExecuteScalar<int, dynamic>("SELECT cc_s_marketplace_clips_count(@SearchTerm)", p) ?? 0;
        var clips = (await _db.LoadData<MarketplaceClip, dynamic>("SELECT * FROM cc_s_marketplace_clips(@SearchTerm,@PageSize,@Offset)", p)).ToList();
        return new MarketplaceSearchResponse { TotalCount = total, Page = req.Page, PageSize = req.PageSize, Clips = clips };
    }

    public Task<IEnumerable<MarketplaceClip>> GetFeaturedClips(int limit = 24) =>
        _db.LoadData<MarketplaceClip, dynamic>("SELECT * FROM cc_s_marketplace_clips(NULL,@Limit,0)", new { Limit = limit });
}
```

### Data/AdminData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Admin;
namespace ClipCore.API.Data;

public class AdminData : IAdminData {
    private readonly ISqlDataAccess _db;
    public AdminData(ISqlDataAccess db) => _db = db;
    public Task<IEnumerable<AdminSellerItem>> GetAllSellers() => _db.LoadData<AdminSellerItem>("SELECT * FROM cc_s_admin_sellers()");
    public Task ApproveSeller(int sellerId) => _db.SaveData("CALL cc_u_seller_approve(@SellerId)", new { SellerId = sellerId });
    public Task RevokeSeller(int sellerId)  => _db.SaveData("CALL cc_u_seller_revoke(@SellerId)",  new { SellerId = sellerId });
    public async Task<PlatformStats> GetPlatformStats() =>
        await _db.LoadSingle<PlatformStats, dynamic>("SELECT * FROM cc_s_platform_stats()", new { }) ?? new PlatformStats();
}
```

### Data/PromoCodeData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;
namespace ClipCore.API.Data;

public class PromoCodeData : IPromoCodeData {
    private readonly ISqlDataAccess _db;
    public PromoCodeData(ISqlDataAccess db) => _db = db;
    public Task<PromoCode?> GetByCodeAsync(string code) => _db.LoadSingle<PromoCode, dynamic>(@"SELECT * FROM ""PromoCodes"" WHERE UPPER(""Code"")=UPPER(@Code)", new { Code = code });
    public Task<IEnumerable<PromoCode>> ListAsync() => _db.LoadData<PromoCode>(@"SELECT * FROM ""PromoCodes"" ORDER BY ""CreatedAt"" DESC");
    public Task AddAsync(PromoCode p) => _db.SaveData(@"INSERT INTO ""PromoCodes"" (""Code"",""DiscountType"",""Value"",""MaxUsages"",""UsageCount"",""ExpiryDate"",""IsActive"",""CreatedAt"") VALUES (@Code,@DiscountType,@Value,@MaxUsages,0,@ExpiryDate,@IsActive,NOW())", new { p.Code, DiscountType = (int)p.DiscountType, p.Value, p.MaxUsages, p.ExpiryDate, p.IsActive });
    public Task IncrementUsageAsync(int id) => _db.SaveData(@"UPDATE ""PromoCodes"" SET ""UsageCount""=""UsageCount""+1 WHERE ""Id""=@Id", new { Id = id });
    public Task DeleteAsync(int id) => _db.SaveData(@"DELETE FROM ""PromoCodes"" WHERE ""Id""=@Id", new { Id = id });
}
```

### Data/SettingsData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;
namespace ClipCore.API.Data;

public class SettingsData : ISettingsData {
    private readonly ISqlDataAccess _db;
    public SettingsData(ISqlDataAccess db) => _db = db;
    public Task<string?> GetValueAsync(string key) => _db.ExecuteScalar<string?, dynamic>(@"SELECT ""Value"" FROM ""Settings"" WHERE ""Key""=@Key", new { Key = key });
    public Task SetValueAsync(string key, string value) => _db.SaveData("CALL cc_u_setting(@Key, @Value)", new { Key = key, Value = value });
    public Task<IEnumerable<Setting>> ListAllAsync() => _db.LoadData<Setting>(@"SELECT * FROM ""Settings""");
}
```

### Data/UsageData.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.Core.Entities;
namespace ClipCore.API.Data;

public class UsageData : IUsageData {
    private readonly ISqlDataAccess _db;
    public UsageData(ISqlDataAccess db) => _db = db;
    public async Task<DailyWatchUsage> GetUsageAsync(string ipAddress, DateOnly date) =>
        await _db.LoadSingle<DailyWatchUsage, dynamic>(@"SELECT * FROM ""DailyWatchUsages"" WHERE ""IpAddress""=@IpAddress AND ""Date""=@Date", new { IpAddress = ipAddress, Date = date })
            ?? new DailyWatchUsage { IpAddress = ipAddress, Date = date };
    public Task IncrementUsageAsync(string ipAddress, DateOnly date, string? userId = null) =>
        _db.SaveData("CALL cc_u_usage_increment(@IpAddress, @Date, @UserId)", new { IpAddress = ipAddress, Date = date, UserId = userId });
}
```

---

## SERVICES

### Services/TokenService.cs
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClipCore.Core.Entities;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace ClipCore.API.Services;

public interface ITokenService { Task<string> GenerateToken(ApplicationUser user); }

public class TokenService : ITokenService {
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager) {
        _config = config; _userManager = userManager;
    }

    public async Task<string> GenerateToken(ApplicationUser user) {
        var roles    = await _userManager.GetRolesAsync(user);
        var sellerId = await GetSellerIdAsync(user.Id);

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
        };
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));
        if (sellerId.HasValue) claims.Add(new Claim("seller_id", sellerId.Value.ToString()));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<int?> GetSellerIdAsync(string userId) {
        await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        return await conn.QueryFirstOrDefaultAsync<int?>(
            @"SELECT ""Id"" FROM ""Sellers"" WHERE ""UserId"" = @UserId", new { UserId = userId });
    }
}
```

### Services/SesEmailService.cs
```csharp
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace ClipCore.API.Services;

public interface IEmailService { Task SendAsync(string to, string subject, string htmlBody); }

public class SesEmailService : IEmailService {
    private readonly IConfiguration _config;
    public SesEmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string to, string subject, string htmlBody) {
        try {
            var client = new AmazonSimpleEmailServiceClient(
                _config["AWS:AccessKeyId"], _config["AWS:SecretAccessKey"],
                Amazon.RegionEndpoint.GetBySystemName(_config["AWS:Region"] ?? "us-east-1"));

            await client.SendEmailAsync(new SendEmailRequest {
                Source = _config["AWS:FromEmail"],
                Destination = new Destination { ToAddresses = new List<string> { to } },
                Message = new Message {
                    Subject = new Content(subject),
                    Body    = new Body { Html = new Content { Charset = "UTF-8", Data = htmlBody } }
                }
            });
        } catch (Exception ex) {
            Console.WriteLine($"[SesEmailService] {ex.Message}");
        }
    }
}
```

### Services/ClipArchiveService.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Data;
namespace ClipCore.API.Services;

// Runs daily at 2am UTC.
// Deletes Mux asset (saves costs), keeps R2 master file.
public class ClipArchiveService : BackgroundService {
    private readonly IServiceProvider _services;
    private readonly ILogger<ClipArchiveService> _logger;

    public ClipArchiveService(IServiceProvider services, ILogger<ClipArchiveService> logger) {
        _services = services; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            var nextRun = DateTime.UtcNow.Date.AddDays(1).AddHours(2);
            await Task.Delay(nextRun - DateTime.UtcNow, ct);
            try { await ArchiveAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "ClipArchiveService failed"); }
        }
    }

    private async Task ArchiveAsync() {
        await using var scope = _services.CreateAsyncScope();
        var clipData = scope.ServiceProvider.GetRequiredService<IClipData>();
        var mux      = scope.ServiceProvider.GetRequiredService<IMuxService>();

        var candidates = await clipData.GetArchiveCandidates(90);
        foreach (var clip in candidates) {
            try {
                if (!string.IsNullOrEmpty(clip.MuxAssetId))
                    await mux.DeleteAssetAsync(clip.MuxAssetId);
                await clipData.ArchiveClip(clip.Id);
                _logger.LogInformation("Archived clip {Id}", clip.Id);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to archive clip {Id}", clip.Id);
            }
        }
    }
}
```

---

## CONTROLLERS

### Controllers/AuthController.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Auth;
using ClipCore.API.Services;
using ClipCore.Core;
using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

public class AuthController : Controller {
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ISellerData   _sellerData;

    public AuthController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm,
        ITokenService ts, ISellerData sd) {
        _userManager = um; _signInManager = sm; _tokenService = ts; _sellerData = sd;
    }

    [HttpPost("Authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model) {
        try {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null) return BadRequest(new { message = "Invalid email or password." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)  return BadRequest(new { message = "Account locked." });
            if (!result.Succeeded)   return BadRequest(new { message = "Invalid email or password." });

            var roles  = await _userManager.GetRolesAsync(user);
            var token  = await _tokenService.GenerateToken(user);
            var seller = await _sellerData.GetSellerProfileByUserId(user.Id);

            return Ok(new AuthenticateResponse {
                Token    = token,
                Email    = user.Email ?? "",
                Role     = roles.FirstOrDefault() ?? "Buyer",
                SellerId = seller?.Id
            });
        } catch (Exception ex) { return BadRequest(new { code = "500", message = ex.Message }); }
    }

    [HttpPost("RegisterSeller")]
    public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerRequest model) {
        try {
            if (ReservedSlugs.IsReserved(model.Slug))
                return BadRequest(new { message = "That storefront slug is reserved." });
            if (await _sellerData.SlugExists(model.Slug))
                return BadRequest(new { message = "That slug is already taken." });

            var user   = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            await _userManager.AddToRoleAsync(user, "Seller");
            var sellerId = await _sellerData.CreateSeller(user.Id);
            await _sellerData.CreateStorefront(sellerId, model.Slug, model.DisplayName);
            var token = await _tokenService.GenerateToken(user);

            return Ok(new AuthenticateResponse { Token = token, Email = user.Email ?? "", Role = "Seller", SellerId = sellerId });
        } catch (Exception ex) { return BadRequest(new { code = "500", message = ex.Message }); }
    }
}
```

### Controllers/ClipController.cs
```csharp
using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Clip;
using ClipCore.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

public class ClipController : Controller {
    private readonly IClipData   _clipData;
    private readonly IMuxService _mux;

    public ClipController(IClipData clipData, IMuxService mux) { _clipData = clipData; _mux = mux; }

    [Authorize(Roles = "Seller")] [HttpGet("GetClips")]
    public async Task<IActionResult> GetClips() => Ok(await _clipData.GetClipsBySeller(SellerId()));

    [Authorize(Roles = "Seller")] [HttpGet("GetClipDetail")]
    public async Task<IActionResult> GetClipDetail(string clipId) {
        var clip = await _clipData.GetClipDetailForSeller(clipId, SellerId());
        return clip is null ? NotFound() : Ok(clip);
    }

    [Authorize(Roles = "Seller")] [HttpGet("GetMuxUploadUrl")]
    public async Task<IActionResult> GetMuxUploadUrl(string clipId) {
        var (uploadUrl, uploadId) = await _mux.CreateDirectUploadAsync();
        await _clipData.SetMuxUploadId(clipId, uploadId);
        return Ok(new { uploadUrl, uploadId });
    }

    [Authorize(Roles = "Seller")] [HttpPost("CreateClip")]
    public async Task<IActionResult> CreateClip([FromBody] CreateClipRequest request) {
        var clipId = await _clipData.CreateClip(SellerId(), request);
        return Ok(new { clipId });
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateClip")]
    public async Task<IActionResult> UpdateClip([FromBody] UpdateClipRequest request) {
        await _clipData.UpdateClip(SellerId(), request); return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateBatchSettings")]
    public async Task<IActionResult> UpdateBatchSettings([FromBody] BatchSettingsRequest request) {
        await _clipData.UpdateBatchSettings(request.CollectionId, SellerId(),
            request.PriceCents, request.PriceCommercialCents, request.AllowGifSale, request.GifPriceCents);
        return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpDelete("DeleteClip")]
    public async Task<IActionResult> DeleteClip(string clipId) {
        var clip = await _clipData.GetClipDetailForSeller(clipId, SellerId());
        if (clip?.MuxAssetId is not null) await _mux.DeleteAssetAsync(clip.MuxAssetId);
        await _clipData.DeleteClip(clipId, SellerId());
        return Ok();
    }

    // Public — for purchase flow. Strips signed playback ID.
    [HttpGet("GetPublicClipDetail")]
    public async Task<IActionResult> GetPublicClipDetail(string clipId) {
        var clip = await _clipData.GetClipDetail(clipId);
        if (clip is null || clip.IsArchived) return NotFound();
        clip.PlaybackIdSigned = string.Empty;   // Never expose to unauthenticated callers
        return Ok(clip);
    }

    private int SellerId() => int.Parse(User.FindFirstValue("seller_id")
        ?? throw new UnauthorizedAccessException("No seller_id claim"));
}
```

### Controllers/SellerController.cs
```csharp
using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Seller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

public class SellerController : Controller {
    private readonly ISellerData _sellerData;
    public SellerController(ISellerData sellerData) => _sellerData = sellerData;

    [Authorize(Roles = "Seller")] [HttpGet("GetSellerProfile")]
    public async Task<IActionResult> GetSellerProfile() {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _sellerData.GetSellerProfileByUserId(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateStorefrontSettings")]
    public async Task<IActionResult> UpdateStorefrontSettings([FromBody] StorefrontSettingsRequest request) {
        await _sellerData.UpdateStorefrontSettings(SellerId(), request); return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpGet("GetSellerSalesStats")]
    public async Task<IActionResult> GetSellerSalesStats() =>
        Ok(await _sellerData.GetSellerSalesStats(SellerId()));

    private int SellerId() => int.Parse(User.FindFirstValue("seller_id")!);
}
```

### Controllers/CollectionController.cs
```csharp
using System.Security.Claims;
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Collection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

public class CollectionController : Controller {
    private readonly ICollectionData _collectionData;
    public CollectionController(ICollectionData collectionData) => _collectionData = collectionData;

    [Authorize(Roles = "Seller")] [HttpGet("GetCollections")]
    public async Task<IActionResult> GetCollections() =>
        Ok(await _collectionData.GetCollectionsBySeller(SellerId()));

    [Authorize(Roles = "Seller")] [HttpGet("GetCollectionDetail")]
    public async Task<IActionResult> GetCollectionDetail(string collectionId) {
        var coll = await _collectionData.GetCollectionDetail(collectionId, SellerId());
        return coll is null ? NotFound() : Ok(coll);
    }

    [Authorize(Roles = "Seller")] [HttpPost("CreateCollection")]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request) {
        var id = await _collectionData.CreateCollection(SellerId(), request);
        return Ok(new { collectionId = id });
    }

    [Authorize(Roles = "Seller")] [HttpPost("UpdateCollection")]
    public async Task<IActionResult> UpdateCollection([FromBody] UpdateCollectionRequest request) {
        await _collectionData.UpdateCollection(SellerId(), request); return Ok();
    }

    [Authorize(Roles = "Seller")] [HttpDelete("DeleteCollection")]
    public async Task<IActionResult> DeleteCollection(string collectionId) {
        await _collectionData.DeleteCollection(collectionId, SellerId()); return Ok();
    }

    private int SellerId() => int.Parse(User.FindFirstValue("seller_id")!);
}
```

### Controllers/MarketplaceController.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Marketplace;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

public class MarketplaceController : Controller {
    private readonly IMarketplaceData _marketplace;
    public MarketplaceController(IMarketplaceData marketplace) => _marketplace = marketplace;

    [HttpGet("GetStorefront")]
    public async Task<IActionResult> GetStorefront(string slug) {
        var sf = await _marketplace.GetStorefront(slug);
        return sf is null ? NotFound() : Ok(sf);
    }

    [HttpGet("GetFeaturedClips")]
    public async Task<IActionResult> GetFeaturedClips(int limit = 24) =>
        Ok(await _marketplace.GetFeaturedClips(limit));

    [HttpPost("SearchClips")]
    public async Task<IActionResult> SearchClips([FromBody] MarketplaceSearchRequest request) =>
        Ok(await _marketplace.SearchClips(request));
}
```

### Controllers/AdminController.cs
```csharp
using ClipCore.API.Interfaces;
using ClipCore.API.Models.Admin;
using ClipCore.API.Models.Purchase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller {
    private readonly IAdminData    _adminData;
    private readonly IPurchaseData _purchaseData;

    public AdminController(IAdminData adminData, IPurchaseData purchaseData) {
        _adminData = adminData; _purchaseData = purchaseData;
    }

    [HttpGet("GetSellers")]       public async Task<IActionResult> GetSellers()       => Ok(await _adminData.GetAllSellers());
    [HttpGet("GetPlatformStats")] public async Task<IActionResult> GetPlatformStats() => Ok(await _adminData.GetPlatformStats());

    [HttpPost("ApproveSeller")]
    public async Task<IActionResult> ApproveSeller([FromBody] ApproveSellerRequest r) {
        await _adminData.ApproveSeller(r.SellerId); return Ok();
    }

    [HttpPost("RevokeSeller")]
    public async Task<IActionResult> RevokeSeller([FromBody] ApproveSellerRequest r) {
        await _adminData.RevokeSeller(r.SellerId); return Ok();
    }

    [HttpGet("GetSellerSalesSummary")]
    public async Task<IActionResult> GetSellerSalesSummary() => Ok(await _purchaseData.GetSellerSalesSummary());

    [HttpGet("GetDailyRevenue")]
    public async Task<IActionResult> GetDailyRevenue(int days = 30) => Ok(await _purchaseData.GetDailyRevenue(days));

    [HttpGet("GetRecentSales")]
    public async Task<IActionResult> GetRecentSales(int count = 20) => Ok(await _purchaseData.GetRecentSales(count));

    [HttpGet("GetAllPurchases")]
    public async Task<IActionResult> GetAllPurchases(int? status, DateTime? since, string? search) =>
        Ok(await _purchaseData.ListFiltered(
            status.HasValue ? (ClipCore.Core.Entities.FulfillmentStatus?)status.Value : null,
            since, search));
}
```

### Controllers/WebhookController.cs
```csharp
using System.Security.Cryptography;
using System.Text;
using ClipCore.API.Interfaces;
using ClipCore.API.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace ClipCore.API.Controllers;

[ApiController]
public class WebhookController : ControllerBase {
    private readonly IConfiguration _config;
    private readonly IClipData _clipData;
    private readonly IPurchaseData _purchaseData;
    private readonly IOrderFulfillmentService _fulfillment;

    public WebhookController(IConfiguration config, IClipData clipData,
        IPurchaseData purchaseData, IOrderFulfillmentService fulfillment) {
        _config = config; _clipData = clipData; _purchaseData = purchaseData; _fulfillment = fulfillment;
    }

    [HttpPost("api/webhooks/mux")]
    public async Task<IActionResult> HandleMux() {
        using var reader = new StreamReader(Request.Body);
        var body      = await reader.ReadToEndAsync();
        var signature = Request.Headers["mux-signature"].ToString();
        var secret    = _config["Mux:WebhookSecret"] ?? "";

        if (!VerifyMuxSignature(body, signature, secret)) return Unauthorized();

        var payload = System.Text.Json.JsonDocument.Parse(body);
        var type    = payload.RootElement.GetProperty("type").GetString();
        var data    = payload.RootElement.GetProperty("data");

        if (type == "video.asset.ready") {
            var muxAssetId   = data.GetProperty("id").GetString()!;
            var uploadId     = data.TryGetProperty("upload_id", out var uid) ? uid.GetString() : null;
            double? duration = data.TryGetProperty("duration", out var d) ? d.GetDouble() : null;

            // Auto-delete clips over 90 seconds
            if (duration > 90) {
                // TODO: delete the asset via MuxService, mark clip as errored
                return Ok();
            }

            // Find clip by upload_id, set Mux data
            // Parse playback IDs — signed vs public policy determines which is which
            if (data.TryGetProperty("playback_ids", out var pbIds) && pbIds.GetArrayLength() > 0) {
                // Implement: look up clip by upload_id, call SetMuxData
            }
        }

        return Ok();
    }

    [HttpPost("api/webhooks/stripe")]
    public async Task<IActionResult> HandleStripe() {
        using var reader = new StreamReader(Request.Body);
        var json   = await reader.ReadToEndAsync();
        var secret = _config["Stripe:WebhookSecret"] ?? "";

        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], secret); }
        catch (StripeException) { return BadRequest(); }

        if (stripeEvent.Type == Events.CheckoutSessionCompleted) {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session is not null)
                await _fulfillment.FulfillOrderAsync(session.Id);
        }

        return Ok();
    }

    private static bool VerifyMuxSignature(string body, string signature, string secret) {
        if (string.IsNullOrEmpty(signature)) return false;
        var parts  = signature.Split(',');
        var tsPart = parts.FirstOrDefault(p => p.StartsWith("t="));
        var v1Part = parts.FirstOrDefault(p => p.StartsWith("v1="));
        if (tsPart is null || v1Part is null) return false;
        var ts       = tsPart[2..];
        var expected = v1Part[3..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{ts}.{body}"))).ToLower();
        return hash == expected;
    }
}
```

---

## FILE UPLOAD — REACT/NEXT.JS

### Video → Mux (`@mux/upchunk`, already in codebase)

```tsx
// components/VideoUploader.tsx
"use client";
import { useState, useRef } from "react";
import UpChunk from "@mux/upchunk";

export function VideoUploader({ clipId, onComplete }: { clipId: string; onComplete: () => void }) {
  const [progress, setProgress] = useState(0);
  const [status, setStatus]     = useState<"idle"|"uploading"|"done"|"error">("idle");
  const inputRef = useRef<HTMLInputElement>(null);

  async function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setStatus("uploading");
    const res = await fetch(`/GetMuxUploadUrl?clipId=${clipId}`, {
      headers: { Authorization: `Bearer ${localStorage.getItem("token")}` },
    });
    const { uploadUrl } = await res.json();
    const upload = UpChunk.createUpload({ endpoint: uploadUrl, file, chunkSize: 5120 });
    upload.on("progress", (e) => setProgress(Math.round(e.detail)));
    upload.on("success",  () => { setStatus("done");  onComplete(); });
    upload.on("error",    () =>   setStatus("error"));
  }

  return (
    <div>
      <input ref={inputRef} type="file" accept="video/*" onChange={handleFile} className="hidden" />
      <button onClick={() => inputRef.current?.click()} disabled={status === "uploading"}>
        {status === "idle" && "Choose video"}
        {status === "uploading" && `Uploading ${progress}%`}
        {status === "done" && "Upload complete"}
        {status === "error" && "Failed — retry"}
      </button>
      {status === "uploading" && (
        <div className="w-full bg-gray-200 rounded h-1.5 mt-2">
          <div className="bg-blue-500 h-1.5 rounded transition-all" style={{ width: `${progress}%` }} />
        </div>
      )}
    </div>
  );
}
```

### Images → R2 (`react-dropzone` + presigned URL)

```bash
npm install react-dropzone
```

Add to `StorageController.cs` in the API:
```csharp
[Authorize(Roles = "Seller")]
[HttpGet("GetUploadUrl")]
public IActionResult GetUploadUrl(string fileName, string contentType) {
    int sellerId = int.Parse(User.FindFirstValue("seller_id")!);
    var key      = $"sellers/{sellerId}/{Guid.NewGuid()}-{fileName}";
    var url      = _r2.GetPresignedUploadUrl(key, contentType);
    return Ok(new { uploadUrl = url, key });
}
```

```tsx
// components/ImageDropzone.tsx
"use client";
import { useCallback, useState } from "react";
import { useDropzone } from "react-dropzone";

export function ImageDropzone({ label, currentUrl, onUploaded, maxSizeMb = 10 }:
  { label: string; currentUrl?: string; onUploaded: (key: string, url: string) => void; maxSizeMb?: number }) {
  const [preview, setPreview]     = useState<string|null>(currentUrl ?? null);
  const [uploading, setUploading] = useState(false);
  const [error, setError]         = useState<string|null>(null);

  const onDrop = useCallback(async (files: File[]) => {
    const file = files[0]; if (!file) return;
    setError(null); setUploading(true); setPreview(URL.createObjectURL(file));
    try {
      const res = await fetch(`/GetUploadUrl?fileName=${encodeURIComponent(file.name)}&contentType=${encodeURIComponent(file.type)}`,
        { headers: { Authorization: `Bearer ${localStorage.getItem("token")}` } });
      const { uploadUrl, key } = await res.json();
      await fetch(uploadUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type } });
      onUploaded(key, `${process.env.NEXT_PUBLIC_R2_PUBLIC_URL}/${key}`);
    } catch { setError("Upload failed."); } finally { setUploading(false); }
  }, [onUploaded]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "image/jpeg": [], "image/png": [], "image/webp": [] },
    maxSize: maxSizeMb * 1024 * 1024,
    multiple: false,
    onDropRejected: (r) => setError(r[0]?.errors[0]?.code === "file-too-large" ? `Max ${maxSizeMb}MB` : "Invalid type"),
  });

  return (
    <div>
      <label className="text-sm font-medium mb-1 block">{label}</label>
      <div {...getRootProps()} className={`border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors ${isDragActive ? "border-blue-500 bg-blue-50" : "border-gray-300 hover:border-gray-400"}`}>
        <input {...getInputProps()} />
        {preview
          ? <img src={preview} alt="" className="mx-auto max-h-32 rounded object-cover" />
          : <p className="text-sm text-gray-500">{isDragActive ? "Drop here" : "Drag & drop or click"}</p>}
        {uploading && <p className="text-sm text-blue-500 mt-2">Uploading…</p>}
        {error    && <p className="text-sm text-red-500 mt-2">{error}</p>}
      </div>
    </div>
  );
}
```

---

## DEPLOY CHECKLIST

```
1. Create project
   [ ] Create ClipCore.API project in solution, reference ClipCore.Core
   [ ] Install all NuGet packages from .csproj

2. PostgreSQL functions
   [ ] Run sql/functions/cc_functions.sql against your Neon database
   [ ] Verify functions exist: SELECT routine_name FROM information_schema.routines
       WHERE routine_schema = 'public' ORDER BY routine_name;

3. Identity EF migration (Identity tables only — all other tables already exist)
   [ ] dotnet ef migrations add InitIdentity
         --context AppIdentityDbContext
         --project ClipCore.API
         --startup-project ClipCore.API
   [ ] dotnet ef database update
         --context AppIdentityDbContext
         --startup-project ClipCore.API

4. DailyWatchUsages unique constraint (required for UPSERT in cc_u_usage_increment)
   [ ] ALTER TABLE "DailyWatchUsages" ADD UNIQUE ("IpAddress", "Date");
       (skip if constraint already exists)

5. Seed roles and admin user
   [ ] Add DataSeeder to Program.cs that creates Admin/Seller/Buyer roles
       and seeds admin@clipcore.com on first run

6. appsettings
   [ ] Update AllowedOrigins for your Next.js domain
   [ ] Add Jwt:Secret (256-bit random string)
   [ ] All other keys (Mux, Stripe, R2, AWS) come from SSM params in prod

7. SSM params to add (new)
   [ ] /clipcore/Jwt__Secret

8. Dockerfile
   [ ] Update ENTRYPOINT from ClipCore.Web.dll to ClipCore.API.dll

9. Services to implement (stubs needed — port from ClipCore.Infrastructure)
   [ ] IMuxService / MuxService    — CreateDirectUploadAsync, DeleteAssetAsync
   [ ] IR2StorageService            — DeleteAsync, GetPresignedUploadUrl
   [ ] IOrderFulfillmentService     — FulfillOrderAsync (Stripe session → purchase)
   [ ] WebhookController.HandleMux  — complete the video.asset.ready handler

10. Test in Swagger
    [ ] POST /Authenticate
    [ ] POST /RegisterSeller
    [ ] GET  /GetClips              (Seller token)
    [ ] GET  /GetStorefront?slug=   (public)
    [ ] GET  /GetPlatformStats      (Admin token)
    [ ] POST /api/webhooks/mux      (test HMAC verification)
```

---

## WHAT IS NOT IN THIS DOCUMENT

These three items exist in the current `ClipCore.Infrastructure` and need to be ported to
`ClipCore.API/Services/`. The logic is already written — it just needs to be moved:

1. **MuxService** — wraps Mux.Csharp.Sdk. `CreateDirectUploadAsync()` creates a direct upload
   URL and returns `(uploadUrl, uploadId)`. `DeleteAssetAsync(assetId)` deletes a Mux asset.

2. **R2StorageService** — wraps AWS S3 SDK pointed at Cloudflare R2 endpoint.
   `DeleteAsync(key)` deletes an object. `GetPresignedUploadUrl(key, contentType)` returns
   a presigned PUT URL for direct browser uploads.

3. **OrderFulfillmentService** — called by the Stripe webhook on `checkout.session.completed`.
   Looks up the Stripe session ID, finds pending purchases, stamps them as Fulfilled, generates
   the download URL, calls `PurchaseData.FulfillPurchase()`. Also calls
   `ClipData.UpdateLastSoldAt()` and applies the 10% platform fee split
   (`PlatformFeeCents = PricePaidCents * 0.10`, `SellerPayoutCents = PricePaidCents * 0.90`).
