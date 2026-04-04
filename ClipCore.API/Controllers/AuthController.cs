using ClipCore.API.Interfaces;
using ClipCore.API.Models.Auth;
using ClipCore.API.Services;
using ClipCore.Core;
using ClipCore.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClipCore.API.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ISellerData   _sellerData;

    public AuthController(
        UserManager<ApplicationUser> um,
        SignInManager<ApplicationUser> sm,
        ITokenService ts,
        ISellerData sd)
    {
        _userManager   = um;
        _signInManager = sm;
        _tokenService  = ts;
        _sellerData    = sd;
    }

    [HttpPost("Authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null) return BadRequest(new { message = "Invalid email or password." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)  return BadRequest(new { message = "Account locked." });
            if (!result.Succeeded)   return BadRequest(new { message = "Invalid email or password." });

            var roles  = await _userManager.GetRolesAsync(user);
            var token  = await _tokenService.GenerateToken(user);
            var seller = await _sellerData.GetSellerProfileByUserId(user.Id);

            return Ok(new AuthenticateResponse
            {
                Token    = token,
                Email    = user.Email ?? "",
                Role     = roles.FirstOrDefault() ?? "Buyer",
                SellerId = seller?.Id
            });
        }
        catch (Exception ex) { return BadRequest(new { code = "500", message = ex.Message }); }
    }

    [HttpPost("RegisterSeller")]
    public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerRequest model)
    {
        try
        {
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
        }
        catch (Exception ex) { return BadRequest(new { code = "500", message = ex.Message }); }
    }
}
