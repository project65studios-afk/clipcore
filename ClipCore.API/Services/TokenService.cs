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

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public async Task<string> GenerateToken(ApplicationUser user)
    {
        var roles    = await _userManager.GetRolesAsync(user);
        var sellerId = await GetSellerIdAsync(user.Id);

        var claims = new List<Claim>
        {
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

    private async Task<int?> GetSellerIdAsync(string userId)
    {
        await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
        return await conn.QueryFirstOrDefaultAsync<int?>(
            @"SELECT ""Id"" FROM ""Sellers"" WHERE ""UserId"" = @UserId", new { UserId = userId });
    }
}
